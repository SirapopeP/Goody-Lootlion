using Lootlion.Application.Abstractions;
using Lootlion.Domain.Entities;
using Lootlion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Services;

public sealed class MissionSpawnService : IMissionSpawnService
{
    private readonly ILootlionDbContext _db;
    private readonly IMissionRecurrenceService _recurrence;

    public MissionSpawnService(ILootlionDbContext db, IMissionRecurrenceService recurrence)
    {
        _db = db;
        _recurrence = recurrence;
    }

    public async Task<MissionInstance> SpawnInitialAsync(MissionTemplate template, Household household, CancellationToken cancellationToken = default)
    {
        var periodKey = _recurrence.BuildPeriodKey(template, household, DateTime.UtcNow);
        return await CreateInstanceAsync(template, household, periodKey, cancellationToken);
    }

    public async Task<MissionInstance?> TrySpawnNextAfterApprovalAsync(
        MissionTemplate template,
        MissionInstance approvedInstance,
        Household household,
        CancellationToken cancellationToken = default)
    {
        if (!_recurrence.HasRecurrence(template))
            return null;

        var nextKey = _recurrence.BuildNextPeriodKey(template, household, approvedInstance.PeriodKey, approvedInstance.CompletedUtc ?? DateTime.UtcNow);

        var exists = await _db.MissionInstances
            .AnyAsync(i => i.TemplateId == template.Id && i.PeriodKey == nextKey, cancellationToken);
        if (exists)
            return null;

        return await CreateInstanceAsync(template, household, nextKey, cancellationToken);
    }

    public async Task<MissionInstance> SpawnManualAsync(MissionTemplate template, Household household, CancellationToken cancellationToken = default)
    {
        if (!template.IsActive)
            throw new InvalidOperationException("Template is not active.");

        var hasOpen = await _db.MissionInstances.AnyAsync(
            i => i.TemplateId == template.Id
                 && (i.Status == MissionInstanceStatus.Available
                     || i.Status == MissionInstanceStatus.Active
                     || i.Status == MissionInstanceStatus.Submitted),
            cancellationToken);
        if (hasOpen)
            throw new InvalidOperationException("An open instance already exists for this template.");

        var periodKey = _recurrence.BuildPeriodKey(template, household, DateTime.UtcNow);
        if (await _db.MissionInstances.AnyAsync(i => i.TemplateId == template.Id && i.PeriodKey == periodKey, cancellationToken))
        {
            periodKey = $"{periodKey}-manual-{Guid.NewGuid():N}";
        }

        return await CreateInstanceAsync(template, household, periodKey, cancellationToken);
    }

    private async Task<MissionInstance> CreateInstanceAsync(
        MissionTemplate template,
        Household household,
        string periodKey,
        CancellationToken cancellationToken)
    {
        var exists = await _db.MissionInstances
            .AnyAsync(i => i.TemplateId == template.Id && i.PeriodKey == periodKey, cancellationToken);
        if (exists)
            throw new InvalidOperationException("An instance for this period already exists.");

        MissionInstanceStatus status;
        Guid? assignee = null;

        if (template.AssignmentMode == MissionAssignmentMode.DirectAssign)
        {
            assignee = template.DefaultAssigneeUserId
                ?? throw new InvalidOperationException("Direct assign template requires a default assignee.");
            status = MissionInstanceStatus.Active;
        }
        else
        {
            status = MissionInstanceStatus.Available;
        }

        var instance = new MissionInstance
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            HouseholdId = template.HouseholdId,
            AssignedToUserId = assignee,
            PeriodKey = periodKey,
            Status = status,
            AvailableFromUtc = _recurrence.ComputeAvailableFromUtc(template, household, periodKey)
        };

        _db.MissionInstances.Add(instance);
        await _db.SaveChangesAsync(cancellationToken);
        return instance;
    }
}
