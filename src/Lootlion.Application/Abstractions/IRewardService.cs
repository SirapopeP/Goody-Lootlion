using Lootlion.Application.Dtos;

namespace Lootlion.Application.Abstractions;

public interface IRewardService
{
    Task<RewardCatalogItemDto> CreateCatalogItemAsync(Guid actorUserId, CreateRewardRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RewardCatalogItemDto>> ListCatalogAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
    Task<RedemptionDto> RedeemAsync(Guid actorUserId, Guid householdId, Guid rewardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RedemptionDto>> ListRedemptionsAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
}
