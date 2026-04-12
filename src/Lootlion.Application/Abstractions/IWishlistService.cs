using Lootlion.Application.Dtos;

namespace Lootlion.Application.Abstractions;

public interface IWishlistService
{
    Task<WishlistItemDto> CreateAsync(Guid actorUserId, CreateWishlistRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WishlistItemDto>> ListAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
    Task<RewardCatalogItemDto> ApproveAsync(Guid actorUserId, Guid wishlistItemId, ApproveWishlistRequest request, CancellationToken cancellationToken = default);
    Task<WishlistItemDto> RejectAsync(Guid actorUserId, Guid wishlistItemId, CancellationToken cancellationToken = default);
}
