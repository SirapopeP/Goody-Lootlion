using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Lootlion.Domain.Entities;
using Lootlion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Services;

public sealed class RewardService : IRewardService
{
    private readonly ILootlionDbContext _db;

    public RewardService(ILootlionDbContext db)
    {
        _db = db;
    }

    public async Task<RewardCatalogItemDto> CreateCatalogItemAsync(Guid actorUserId, CreateRewardRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureParentAsync(actorUserId, request.HouseholdId, cancellationToken);

        var item = new RewardCatalogItem
        {
            Id = Guid.NewGuid(),
            HouseholdId = request.HouseholdId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CostCoin = request.CostCoin,
            CreatedByUserId = actorUserId,
            SourceWishlistItemId = null
        };
        _db.RewardCatalogItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<IReadOnlyList<RewardCatalogItemDto>> ListCatalogAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(userId, householdId, cancellationToken);

        var rows = await _db.RewardCatalogItems
            .AsNoTracking()
            .Where(r => r.HouseholdId == householdId)
            .OrderBy(r => r.Title)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<RedemptionDto> RedeemAsync(Guid actorUserId, Guid householdId, Guid rewardId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(actorUserId, householdId, cancellationToken);

        var reward = await _db.RewardCatalogItems
            .FirstOrDefaultAsync(r => r.Id == rewardId && r.HouseholdId == householdId, cancellationToken)
            ?? throw new InvalidOperationException("Reward not found.");

        var balance = await _db.LedgerEntries
            .Where(e => e.HouseholdId == householdId && e.UserId == actorUserId)
            .SumAsync(e => (long?)e.DeltaCoin, cancellationToken) ?? 0;

        if (balance < reward.CostCoin)
            throw new InvalidOperationException("Insufficient coin balance.");

        var redemption = new Redemption
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            UserId = actorUserId,
            RewardCatalogItemId = reward.Id,
            CoinSpent = reward.CostCoin,
            Status = RedemptionStatus.Fulfilled,
            CreatedUtc = DateTime.UtcNow
        };
        _db.Redemptions.Add(redemption);

        _db.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            UserId = actorUserId,
            DeltaCoin = -reward.CostCoin,
            DeltaExp = 0,
            EntryType = LedgerEntryType.RedemptionSpend,
            ReferenceId = redemption.Id,
            ReferenceType = nameof(Redemption),
            CreatedUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return MapRedemption(redemption);
    }

    public async Task<IReadOnlyList<RedemptionDto>> ListRedemptionsAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(userId, householdId, cancellationToken);

        var rows = await _db.Redemptions
            .AsNoTracking()
            .Where(r => r.HouseholdId == householdId && r.UserId == userId)
            .OrderByDescending(r => r.CreatedUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(MapRedemption).ToList();
    }

    private static RewardCatalogItemDto Map(RewardCatalogItem r) =>
        new(r.Id, r.HouseholdId, r.Title, r.Description, r.CostCoin, r.CreatedByUserId, r.SourceWishlistItemId);

    private static RedemptionDto MapRedemption(Redemption r) =>
        new(r.Id, r.RewardCatalogItemId, r.CoinSpent, r.Status.ToString(), r.CreatedUtc);

    private async Task EnsureMemberAsync(Guid userId, Guid householdId, CancellationToken cancellationToken)
    {
        var ok = await _db.HouseholdMembers
            .AsNoTracking()
            .AnyAsync(m => m.HouseholdId == householdId && m.UserId == userId, cancellationToken);
        if (!ok)
            throw new InvalidOperationException("Household not found or access denied.");
    }

    private async Task EnsureParentAsync(Guid userId, Guid householdId, CancellationToken cancellationToken)
    {
        var row = await _db.HouseholdMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.UserId == userId, cancellationToken);
        if (row is null || row.Role != MemberRole.Parent)
            throw new InvalidOperationException("Only a parent can perform this action.");
    }
}
