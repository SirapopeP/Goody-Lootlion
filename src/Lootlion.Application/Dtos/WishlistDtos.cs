namespace Lootlion.Application.Dtos;

public record WishlistItemDto(
    Guid Id,
    Guid HouseholdId,
    Guid RequestedByUserId,
    string Title,
    string? Description,
    int? SuggestedCoinCost,
    string Status,
    Guid? ApprovedRewardId,
    DateTime CreatedUtc);

public record CreateWishlistRequest(Guid HouseholdId, string Title, string? Description, int? SuggestedCoinCost);

public record ApproveWishlistRequest(int FinalCostCoin);
