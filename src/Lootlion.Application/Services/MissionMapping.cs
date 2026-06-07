using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Lootlion.Domain.Entities;
using Lootlion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Services;

internal static class MissionMapping
{
    public static MissionTemplateDto ToTemplateDto(MissionTemplate t, bool canSpawnNextRound) =>
        new(
            t.Id,
            t.HouseholdId,
            t.CreatedByUserId,
            t.Title,
            t.Description,
            t.RewardExp,
            t.RewardCoin,
            t.RequiresApproval,
            t.AssignmentMode,
            t.DefaultAssigneeUserId,
            t.RecurrenceKind,
            t.RecurrenceIntervalDays,
            t.RecurrenceDayOfWeek,
            t.RecurrenceDayOfMonth,
            t.IsActive,
            canSpawnNextRound,
            t.CreatedUtc,
            t.CancelledUtc);

    public static MissionInstanceDto ToInstanceDto(MissionInstance i, MissionTemplate t) =>
        new(
            i.Id,
            i.TemplateId,
            i.HouseholdId,
            i.AssignedToUserId,
            i.PeriodKey,
            i.Status,
            i.AvailableFromUtc,
            i.SubmittedUtc,
            i.CompletedUtc,
            t.Title,
            t.Description,
            t.RewardExp,
            t.RewardCoin,
            t.RequiresApproval,
            t.AssignmentMode,
            t.RecurrenceKind);

    public static async Task<bool> CanSpawnNextRoundAsync(
        ILootlionDbContext db,
        IMissionRecurrenceService recurrence,
        MissionTemplate template,
        CancellationToken cancellationToken)
    {
        if (!template.IsActive || !recurrence.HasRecurrence(template))
            return false;

        var hasOpen = await db.MissionInstances.AnyAsync(
            i => i.TemplateId == template.Id
                 && (i.Status == MissionInstanceStatus.Available
                     || i.Status == MissionInstanceStatus.Active
                     || i.Status == MissionInstanceStatus.Submitted),
            cancellationToken);

        return !hasOpen;
    }
}
