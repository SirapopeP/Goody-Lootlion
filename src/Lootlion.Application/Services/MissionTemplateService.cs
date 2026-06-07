using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Lootlion.Domain.Entities;
using Lootlion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Services;

public sealed class MissionTemplateService : IMissionTemplateService
{
    private readonly ILootlionDbContext _db;
    private readonly IMissionRecurrenceService _recurrence;
    private readonly IMissionSpawnService _spawn;

    public MissionTemplateService(
        ILootlionDbContext db,
        IMissionRecurrenceService recurrence,
        IMissionSpawnService spawn)
    {
        _db = db;
        _recurrence = recurrence;
        _spawn = spawn;
    }

    public async Task<MissionTemplateDto> CreateAsync(
        Guid actorUserId,
        CreateMissionTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, request.HouseholdId, cancellationToken);
        ValidateCreateRequest(request);

        if (request.AssignmentMode == MissionAssignmentMode.DirectAssign)
        {
            var assigneeId = request.DefaultAssigneeUserId
                ?? throw new InvalidOperationException("Direct assign requires a default assignee.");
            await HouseholdAccess.EnsureMemberAsync(_db, assigneeId, request.HouseholdId, cancellationToken);
        }

        var household = await _db.Households
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == request.HouseholdId, cancellationToken)
            ?? throw new InvalidOperationException("Household not found.");

        var template = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            HouseholdId = request.HouseholdId,
            CreatedByUserId = actorUserId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            RewardExp = request.RewardExp,
            RewardCoin = request.RewardCoin,
            RequiresApproval = request.RequiresApproval,
            AssignmentMode = request.AssignmentMode,
            DefaultAssigneeUserId = request.AssignmentMode == MissionAssignmentMode.DirectAssign
                ? request.DefaultAssigneeUserId
                : null,
            RecurrenceKind = request.RecurrenceKind,
            RecurrenceIntervalDays = request.RecurrenceIntervalDays,
            RecurrenceDayOfWeek = request.RecurrenceDayOfWeek,
            RecurrenceDayOfMonth = request.RecurrenceDayOfMonth,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };

        _db.MissionTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);

        await _spawn.SpawnInitialAsync(template, household, cancellationToken);

        var canSpawn = await MissionMapping.CanSpawnNextRoundAsync(_db, _recurrence, template, cancellationToken);
        return MissionMapping.ToTemplateDto(template, canSpawn);
    }

    public async Task<IReadOnlyList<MissionTemplateDto>> ListForHouseholdAsync(
        Guid userId,
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureMemberAsync(_db, userId, householdId, cancellationToken);

        var rows = await _db.MissionTemplates
            .AsNoTracking()
            .Where(t => t.HouseholdId == householdId && t.IsActive)
            .OrderByDescending(t => t.CreatedUtc)
            .ToListAsync(cancellationToken);

        var result = new List<MissionTemplateDto>();
        foreach (var t in rows)
        {
            var canSpawn = await MissionMapping.CanSpawnNextRoundAsync(_db, _recurrence, t, cancellationToken);
            result.Add(MissionMapping.ToTemplateDto(t, canSpawn));
        }

        return result;
    }

    public async Task<MissionTemplateDto> CancelAsync(
        Guid actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await _db.MissionTemplates.FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken)
            ?? throw new InvalidOperationException("Mission template not found.");

        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, template.HouseholdId, cancellationToken);

        if (!template.IsActive)
            throw new InvalidOperationException("Template is already cancelled.");

        template.IsActive = false;
        template.CancelledUtc = DateTime.UtcNow;

        var openInstances = await _db.MissionInstances
            .Where(i => i.TemplateId == templateId
                        && (i.Status == MissionInstanceStatus.Available
                            || i.Status == MissionInstanceStatus.Active
                            || i.Status == MissionInstanceStatus.Submitted))
            .ToListAsync(cancellationToken);

        foreach (var instance in openInstances)
            instance.Status = MissionInstanceStatus.Cancelled;

        await _db.SaveChangesAsync(cancellationToken);
        return MissionMapping.ToTemplateDto(template, false);
    }

    public async Task<MissionInstanceDto> SpawnNextRoundAsync(
        Guid actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await _db.MissionTemplates.FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken)
            ?? throw new InvalidOperationException("Mission template not found.");

        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, template.HouseholdId, cancellationToken);

        var household = await _db.Households
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == template.HouseholdId, cancellationToken)
            ?? throw new InvalidOperationException("Household not found.");

        var instance = await _spawn.SpawnManualAsync(template, household, cancellationToken);
        return MissionMapping.ToInstanceDto(instance, template);
    }

    private static void ValidateCreateRequest(CreateMissionTemplateRequest request)
    {
        if (request.AssignmentMode == MissionAssignmentMode.DirectAssign && request.DefaultAssigneeUserId is null)
            throw new InvalidOperationException("Direct assign requires a default assignee.");

        if (request.AssignmentMode == MissionAssignmentMode.BoardClaim && request.DefaultAssigneeUserId is not null)
            throw new InvalidOperationException("Board claim must not specify a default assignee.");

        if (request.RecurrenceKind == MissionRecurrenceKind.IntervalDays
            && (request.RecurrenceIntervalDays is null or < 1))
            throw new InvalidOperationException("Interval recurrence requires RecurrenceIntervalDays >= 1.");

        if (request.RecurrenceKind == MissionRecurrenceKind.Weekly && request.RecurrenceDayOfWeek is null)
            throw new InvalidOperationException("Weekly recurrence requires RecurrenceDayOfWeek.");

        if (request.RecurrenceKind == MissionRecurrenceKind.Monthly && request.RecurrenceDayOfMonth is null)
            throw new InvalidOperationException("Monthly recurrence requires RecurrenceDayOfMonth.");
    }
}
