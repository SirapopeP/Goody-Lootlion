using Lootlion.Domain.Enums;

namespace Lootlion.Domain.Entities;

public class WishlistItem
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SuggestedCoinCost { get; set; }
    public WishlistStatus Status { get; set; }
    public Guid? ApprovedRewardId { get; set; }
    public DateTime CreatedUtc { get; set; }
}
