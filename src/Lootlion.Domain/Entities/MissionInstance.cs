using Lootlion.Domain.Enums;

namespace Lootlion.Domain.Entities;

public class MissionInstance
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string PeriodKey { get; set; } = string.Empty;
    public MissionInstanceStatus Status { get; set; }
    public DateTime AvailableFromUtc { get; set; }
    public DateTime? SubmittedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }

    public MissionTemplate Template { get; set; } = null!;
    public Household Household { get; set; } = null!;
}
