using Lootlion.Domain.Enums;

namespace Lootlion.Application.Dtos;

public record MissionDto(
    Guid Id,
    Guid HouseholdId,
    Guid AssignedByUserId,
    Guid AssignedToUserId,
    string Title,
    string? Description,
    int RewardExp,
    int RewardCoin,
    bool RequiresApproval,
    MissionStatus Status,
    DateTime CreatedUtc,
    DateTime? SubmittedUtc,
    DateTime? CompletedUtc);

public record CreateMissionRequest(
    Guid HouseholdId,
    Guid AssignedToUserId,
    string Title,
    string? Description,
    int RewardExp,
    int RewardCoin,
    bool RequiresApproval);
