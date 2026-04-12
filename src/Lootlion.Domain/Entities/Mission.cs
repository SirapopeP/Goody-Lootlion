using Lootlion.Domain.Enums;

namespace Lootlion.Domain.Entities;

public class Mission
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid AssignedByUserId { get; set; }
    public Guid AssignedToUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RewardExp { get; set; }
    public int RewardCoin { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public MissionStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? SubmittedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }

    public Household Household { get; set; } = null!;
}
