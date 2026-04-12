using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Lootlion.Domain.Entities;
using Lootlion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Services;

public sealed class WishlistService : IWishlistService
{
    private readonly ILootlionDbContext _db;

    public WishlistService(ILootlionDbContext db)
    {
        _db = db;
    }

    public async Task<WishlistItemDto> CreateAsync(Guid actorUserId, CreateWishlistRequest request, CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureMemberAsync(_db, actorUserId, request.HouseholdId, cancellationToken);

        var item = new WishlistItem
        {
            Id = Guid.NewGuid(),
            HouseholdId = request.HouseholdId,
            RequestedByUserId = actorUserId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            SuggestedCoinCost = request.SuggestedCoinCost,
            Status = WishlistStatus.Pending,
            CreatedUtc = DateTime.UtcNow
        };
        _db.WishlistItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<IReadOnlyList<WishlistItemDto>> ListAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureMemberAsync(_db, userId, householdId, cancellationToken);

        var rows = await _db.WishlistItems
            .AsNoTracking()
            .Where(w => w.HouseholdId == householdId)
            .OrderByDescending(w => w.CreatedUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<RewardCatalogItemDto> ApproveAsync(Guid actorUserId, Guid wishlistItemId, ApproveWishlistRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _db.WishlistItems.FirstOrDefaultAsync(w => w.Id == wishlistItemId, cancellationToken)
            ?? throw new InvalidOperationException("Wishlist item not found.");

        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, item.HouseholdId, cancellationToken);

        if (item.Status != WishlistStatus.Pending)
            throw new InvalidOperationException("Wishlist item is not pending.");

        if (request.FinalCostCoin < 0)
            throw new InvalidOperationException("Final cost must be non-negative.");

        var reward = new RewardCatalogItem
        {
            Id = Guid.NewGuid(),
            HouseholdId = item.HouseholdId,
            Title = item.Title,
            Description = item.Description,
            CostCoin = request.FinalCostCoin,
            CreatedByUserId = actorUserId,
            SourceWishlistItemId = item.Id
        };
        _db.RewardCatalogItems.Add(reward);

        item.Status = WishlistStatus.Approved;
        item.ApprovedRewardId = reward.Id;

        await _db.SaveChangesAsync(cancellationToken);
        return new RewardCatalogItemDto(
            reward.Id,
            reward.HouseholdId,
            reward.Title,
            reward.Description,
            reward.CostCoin,
            reward.CreatedByUserId,
            reward.SourceWishlistItemId);
    }

    public async Task<WishlistItemDto> RejectAsync(Guid actorUserId, Guid wishlistItemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.WishlistItems.FirstOrDefaultAsync(w => w.Id == wishlistItemId, cancellationToken)
            ?? throw new InvalidOperationException("Wishlist item not found.");

        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, item.HouseholdId, cancellationToken);

        if (item.Status != WishlistStatus.Pending)
            throw new InvalidOperationException("Wishlist item is not pending.");

        item.Status = WishlistStatus.Rejected;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    private static WishlistItemDto Map(WishlistItem w) =>
        new(
            w.Id,
            w.HouseholdId,
            w.RequestedByUserId,
            w.Title,
            w.Description,
            w.SuggestedCoinCost,
            w.Status.ToString(),
            w.ApprovedRewardId,
            w.CreatedUtc);

}
