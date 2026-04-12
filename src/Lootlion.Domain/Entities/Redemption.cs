using Lootlion.Domain.Enums;

namespace Lootlion.Domain.Entities;

public class Redemption
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid UserId { get; set; }
    public Guid RewardCatalogItemId { get; set; }
    public int CoinSpent { get; set; }
    public RedemptionStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; }
}
