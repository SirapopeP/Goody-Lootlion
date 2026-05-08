using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Lootlion.Domain.Entities;
using Lootlion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Services;

public sealed class HouseholdService : IHouseholdService
{
    private readonly ILootlionDbContext _db;
    private readonly IUserProfileReader _users;

    public HouseholdService(ILootlionDbContext db, IUserProfileReader users)
    {
        _db = db;
        _users = users;
    }

    public async Task<HouseholdDto> CreateAsync(Guid actorUserId, CreateHouseholdRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var household = new Household
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedUtc = now,
            AllowChildPickJoin = true
        };
        _db.Households.Add(household);
        _db.HouseholdMembers.Add(new HouseholdMember
        {
            Id = Guid.NewGuid(),
            HouseholdId = household.Id,
            UserId = actorUserId,
            Role = MemberRole.Parent,
            Status = HouseholdMembershipStatus.Active,
            JoinedUtc = now
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new HouseholdDto(household.Id, household.Name, household.CreatedUtc);
    }

    public async Task<IReadOnlyList<HouseholdDto>> ListOpenForChildJoinAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Households
            .AsNoTracking()
            .Where(h => h.AllowChildPickJoin)
            .OrderBy(h => h.Name)
            .Select(h => new HouseholdDto(h.Id, h.Name, h.CreatedUtc))
            .ToListAsync(cancellationToken);
        return list;
    }

    public async Task JoinHouseholdAsParentAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Households.AsNoTracking().AnyAsync(h => h.Id == householdId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Household not found.");

        var already = await _db.HouseholdMembers
            .AnyAsync(m => m.HouseholdId == householdId && m.UserId == userId, cancellationToken);
        if (already)
            return;

        _db.HouseholdMembers.Add(new HouseholdMember
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            UserId = userId,
            Role = MemberRole.Parent,
            Status = HouseholdMembershipStatus.Pending,
            JoinedUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HouseholdMineDto>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.HouseholdMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                _db.Households.AsNoTracking(),
                m => m.HouseholdId,
                h => h.Id,
                (m, h) => new HouseholdMineDto(
                    h.Id,
                    h.Name,
                    h.CreatedUtc,
                    m.Status == HouseholdMembershipStatus.Active ? "Active" : "Pending"))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<IReadOnlyList<HouseholdMemberDto>> GetMembersAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureMemberAsync(_db, userId, householdId, cancellationToken);

        var members = await _db.HouseholdMembers
            .AsNoTracking()
            .Where(m => m.HouseholdId == householdId)
            .ToListAsync(cancellationToken);

        var profiles = await _users.GetManyAsync(members.Select(m => m.UserId), cancellationToken);

        return members
            .Select(m =>
            {
                profiles.TryGetValue(m.UserId, out var p);
                var email = p?.Email ?? string.Empty;
                var name = p?.DisplayName ?? string.Empty;
                var role = m.Role == MemberRole.Parent ? "Parent" : "Child";
                var status = m.Status == HouseholdMembershipStatus.Active ? "Active" : "Pending";
                return new HouseholdMemberDto(m.UserId, name, email, role, status);
            })
            .OrderBy(m => m.DisplayName)
            .ToList();
    }

    public async Task AddMemberAsync(Guid actorUserId, Guid householdId, Guid memberUserId, MemberRole role, CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, householdId, cancellationToken);

        var exists = await _db.HouseholdMembers
            .AnyAsync(m => m.HouseholdId == householdId && m.UserId == memberUserId, cancellationToken);
        if (exists)
            throw new InvalidOperationException("User is already a member.");

        _db.HouseholdMembers.Add(new HouseholdMember
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            UserId = memberUserId,
            Role = role,
            Status = HouseholdMembershipStatus.Active,
            JoinedUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveMemberAsync(Guid actorUserId, Guid householdId, Guid memberUserId, CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, householdId, cancellationToken);

        if (actorUserId == memberUserId)
            throw new InvalidOperationException("Cannot approve your own membership.");

        var row = await _db.HouseholdMembers
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.UserId == memberUserId, cancellationToken);
        if (row is null)
            throw new InvalidOperationException("Member not found.");

        if (row.Status != HouseholdMembershipStatus.Pending)
            throw new InvalidOperationException("Member is not pending approval.");

        row.Status = HouseholdMembershipStatus.Active;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectMemberAsync(Guid actorUserId, Guid householdId, Guid memberUserId, CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, householdId, cancellationToken);

        if (actorUserId == memberUserId)
            throw new InvalidOperationException("Cannot reject your own membership.");

        var row = await _db.HouseholdMembers
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.UserId == memberUserId, cancellationToken);
        if (row is null)
            throw new InvalidOperationException("Member not found.");

        if (row.Status != HouseholdMembershipStatus.Pending)
            throw new InvalidOperationException("Member is not pending approval.");

        _db.HouseholdMembers.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSearchHitDto>> SearchInviteCandidatesAsync(
        Guid actorUserId,
        Guid householdId,
        string query,
        CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, householdId, cancellationToken);

        var memberIds = await _db.HouseholdMembers
            .AsNoTracking()
            .Where(m => m.HouseholdId == householdId)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var memberSet = memberIds.ToHashSet();
        var hits = await _users.SearchByDisplayNameOrEmailAsync(query, 40, cancellationToken);

        return hits
            .Where(h => h.UserId != actorUserId && !memberSet.Contains(h.UserId))
            .Take(20)
            .ToList();
    }

    public async Task DeleteHouseholdAsync(Guid actorUserId, Guid householdId, string confirmationName, CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, householdId, cancellationToken);

        var household = await _db.Households.FirstOrDefaultAsync(h => h.Id == householdId, cancellationToken);
        if (household is null)
            throw new InvalidOperationException("Household not found.");

        if (!string.Equals(household.Name.Trim(), confirmationName.Trim(), StringComparison.Ordinal))
            throw new InvalidOperationException("Confirmation name does not match.");

        await _db.LedgerEntries.Where(e => e.HouseholdId == householdId).ExecuteDeleteAsync(cancellationToken);
        await _db.Redemptions.Where(r => r.HouseholdId == householdId).ExecuteDeleteAsync(cancellationToken);
        await _db.WishlistItems.Where(w => w.HouseholdId == householdId).ExecuteDeleteAsync(cancellationToken);
        await _db.RewardCatalogItems.Where(r => r.HouseholdId == householdId).ExecuteDeleteAsync(cancellationToken);

        _db.Households.Remove(household);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
