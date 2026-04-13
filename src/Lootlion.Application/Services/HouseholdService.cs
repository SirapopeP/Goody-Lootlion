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
            JoinedUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HouseholdDto>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var ids = await _db.HouseholdMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.HouseholdId)
            .ToListAsync(cancellationToken);

        var list = await _db.Households
            .AsNoTracking()
            .Where(h => ids.Contains(h.Id))
            .OrderBy(h => h.Name)
            .Select(h => new HouseholdDto(h.Id, h.Name, h.CreatedUtc))
            .ToListAsync(cancellationToken);

        return list;
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
                return new HouseholdMemberDto(m.UserId, name, email, role);
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
            JoinedUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
