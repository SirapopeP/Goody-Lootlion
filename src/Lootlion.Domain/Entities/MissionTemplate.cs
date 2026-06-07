using Lootlion.Domain.Enums;

namespace Lootlion.Domain.Entities;

public class MissionTemplate
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RewardExp { get; set; }
    public int RewardCoin { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public MissionAssignmentMode AssignmentMode { get; set; }
    public Guid? DefaultAssigneeUserId { get; set; }
    public MissionRecurrenceKind RecurrenceKind { get; set; }
    public int? RecurrenceIntervalDays { get; set; }
    public DayOfWeek? RecurrenceDayOfWeek { get; set; }
    public int? RecurrenceDayOfMonth { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
    public DateTime? CancelledUtc { get; set; }

    public Household Household { get; set; } = null!;
    public ICollection<MissionInstance> Instances { get; set; } = new List<MissionInstance>();
}
