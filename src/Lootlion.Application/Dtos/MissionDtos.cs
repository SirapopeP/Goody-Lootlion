using Lootlion.Domain.Enums;

namespace Lootlion.Application.Dtos;

public record MissionTemplateDto(
    Guid Id,
    Guid HouseholdId,
    Guid CreatedByUserId,
    string Title,
    string? Description,
    int RewardExp,
    int RewardCoin,
    bool RequiresApproval,
    MissionAssignmentMode AssignmentMode,
    Guid? DefaultAssigneeUserId,
    MissionRecurrenceKind RecurrenceKind,
    int? RecurrenceIntervalDays,
    DayOfWeek? RecurrenceDayOfWeek,
    int? RecurrenceDayOfMonth,
    bool IsActive,
    bool CanSpawnNextRound,
    DateTime CreatedUtc,
    DateTime? CancelledUtc);

public record MissionInstanceDto(
    Guid Id,
    Guid TemplateId,
    Guid HouseholdId,
    Guid? AssignedToUserId,
    string PeriodKey,
    MissionInstanceStatus Status,
    DateTime AvailableFromUtc,
    DateTime? SubmittedUtc,
    DateTime? CompletedUtc,
    string Title,
    string? Description,
    int RewardExp,
    int RewardCoin,
    bool RequiresApproval,
    MissionAssignmentMode AssignmentMode,
    MissionRecurrenceKind RecurrenceKind);

public record CreateMissionTemplateRequest(
    Guid HouseholdId,
    string Title,
    string? Description,
    int RewardExp,
    int RewardCoin,
    bool RequiresApproval,
    MissionAssignmentMode AssignmentMode,
    Guid? DefaultAssigneeUserId,
    MissionRecurrenceKind RecurrenceKind,
    int? RecurrenceIntervalDays,
    DayOfWeek? RecurrenceDayOfWeek,
    int? RecurrenceDayOfMonth);
