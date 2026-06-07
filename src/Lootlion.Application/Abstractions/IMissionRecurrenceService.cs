using Lootlion.Domain.Entities;

namespace Lootlion.Application.Abstractions;

public interface IMissionRecurrenceService
{
    string BuildPeriodKey(MissionTemplate template, Household household, DateTime utcNow);
    string BuildNextPeriodKey(MissionTemplate template, Household household, string currentPeriodKey, DateTime completedUtc);
    DateTime ComputeAvailableFromUtc(MissionTemplate template, Household household, string periodKey);
    bool HasRecurrence(MissionTemplate template);
}
