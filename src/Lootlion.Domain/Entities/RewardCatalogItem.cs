namespace Lootlion.Domain.Entities;

public class RewardCatalogItem
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CostCoin { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? SourceWishlistItemId { get; set; }
}
