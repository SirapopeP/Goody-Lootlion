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
            CreatedUtc = now
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
        await EnsureMemberAsync(userId, householdId, cancellationToken);

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
        var actor = await _db.HouseholdMembers
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.UserId == actorUserId, cancellationToken);
        if (actor is null || actor.Role != MemberRole.Parent)
            throw new InvalidOperationException("Only a parent in this household can add members.");

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

    private async Task EnsureMemberAsync(Guid userId, Guid householdId, CancellationToken cancellationToken)
    {
        var ok = await _db.HouseholdMembers
            .AsNoTracking()
            .AnyAsync(m => m.HouseholdId == householdId && m.UserId == userId, cancellationToken);
        if (!ok)
            throw new InvalidOperationException("Household not found or access denied.");
    }
}
