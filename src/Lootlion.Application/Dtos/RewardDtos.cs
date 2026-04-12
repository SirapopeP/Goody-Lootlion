namespace Lootlion.Application.Dtos;

public record RewardCatalogItemDto(
    Guid Id,
    Guid HouseholdId,
    string Title,
    string? Description,
    int CostCoin,
    Guid CreatedByUserId,
    Guid? SourceWishlistItemId);

public record CreateRewardRequest(Guid HouseholdId, string Title, string? Description, int CostCoin);

public record RedemptionDto(
    Guid Id,
    Guid RewardCatalogItemId,
    int CoinSpent,
    string Status,
    DateTime CreatedUtc);
