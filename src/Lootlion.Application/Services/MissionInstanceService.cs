using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Lootlion.Domain.Entities;
using Lootlion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Services;

public sealed class MissionInstanceService : IMissionInstanceService
{
    private readonly ILootlionDbContext _db;
    private readonly IMissionSpawnService _spawn;

    public MissionInstanceService(ILootlionDbContext db, IMissionSpawnService spawn)
    {
        _db = db;
        _spawn = spawn;
    }

    public async Task<IReadOnlyList<MissionInstanceDto>> ListBoardAsync(
        Guid userId,
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureMemberAsync(_db, userId, householdId, cancellationToken);

        var rows = await QueryWithTemplate()
            .Where(x => x.Instance.HouseholdId == householdId
                        && x.Instance.Status == MissionInstanceStatus.Available
                        && x.Template.AssignmentMode == MissionAssignmentMode.BoardClaim
                        && x.Template.IsActive)
            .OrderByDescending(x => x.Instance.AvailableFromUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(x => MissionMapping.ToInstanceDto(x.Instance, x.Template)).ToList();
    }

    public async Task<IReadOnlyList<MissionInstanceDto>> ListMineAsync(
        Guid userId,
        Guid householdId,
        MissionInstanceStatus? status,
        CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureMemberAsync(_db, userId, householdId, cancellationToken);

        var query = QueryWithTemplate()
            .Where(x => x.Instance.HouseholdId == householdId && x.Instance.AssignedToUserId == userId);

        if (status.HasValue)
            query = query.Where(x => x.Instance.Status == status.Value);
        else
            query = query.Where(x => x.Instance.Status != MissionInstanceStatus.Cancelled);

        var rows = await query
            .OrderByDescending(x => x.Instance.AvailableFromUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(x => MissionMapping.ToInstanceDto(x.Instance, x.Template)).ToList();
    }

    public async Task<IReadOnlyList<MissionInstanceDto>> ListPendingAsync(
        Guid userId,
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureParentAsync(_db, userId, householdId, cancellationToken);

        var rows = await QueryWithTemplate()
            .Where(x => x.Instance.HouseholdId == householdId && x.Instance.Status == MissionInstanceStatus.Submitted)
            .OrderByDescending(x => x.Instance.SubmittedUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(x => MissionMapping.ToInstanceDto(x.Instance, x.Template)).ToList();
    }

    public async Task<MissionInstanceDto> ClaimAsync(
        Guid actorUserId,
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var row = await LoadInstanceWithTemplate(instanceId, cancellationToken);
        await HouseholdAccess.EnsureMemberAsync(_db, actorUserId, row.Instance.HouseholdId, cancellationToken);

        if (row.Template.AssignmentMode != MissionAssignmentMode.BoardClaim)
            throw new InvalidOperationException("This mission is not available for board claim.");

        if (row.Instance.Status != MissionInstanceStatus.Available)
            throw new InvalidOperationException("Mission is not available to claim.");

        row.Instance.AssignedToUserId = actorUserId;
        row.Instance.Status = MissionInstanceStatus.Active;

        await _db.SaveChangesAsync(cancellationToken);
        return MissionMapping.ToInstanceDto(row.Instance, row.Template);
    }

    public async Task<MissionInstanceDto> SubmitAsync(
        Guid actorUserId,
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var row = await LoadInstanceWithTemplate(instanceId, cancellationToken);
        await HouseholdAccess.EnsureMemberAsync(_db, actorUserId, row.Instance.HouseholdId, cancellationToken);

        if (row.Instance.AssignedToUserId != actorUserId)
            throw new InvalidOperationException("Only the assignee can submit this mission.");

        if (row.Instance.Status != MissionInstanceStatus.Active)
            throw new InvalidOperationException("Mission cannot be submitted in its current state.");

        var now = DateTime.UtcNow;
        row.Instance.SubmittedUtc = now;

        if (!row.Template.RequiresApproval)
        {
            row.Instance.Status = MissionInstanceStatus.Approved;
            row.Instance.CompletedUtc = now;
            await AppendMissionLedgerAsync(row.Instance, row.Template, cancellationToken);
            await TrySpawnNextAsync(row, cancellationToken);
        }
        else
        {
            row.Instance.Status = MissionInstanceStatus.Submitted;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return MissionMapping.ToInstanceDto(row.Instance, row.Template);
    }

    public async Task<MissionInstanceDto> ApproveAsync(
        Guid actorUserId,
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var row = await LoadInstanceWithTemplate(instanceId, cancellationToken);
        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, row.Instance.HouseholdId, cancellationToken);

        if (row.Instance.Status != MissionInstanceStatus.Submitted)
            throw new InvalidOperationException("Mission is not waiting for approval.");

        row.Instance.Status = MissionInstanceStatus.Approved;
        row.Instance.CompletedUtc = DateTime.UtcNow;
        await AppendMissionLedgerAsync(row.Instance, row.Template, cancellationToken);
        await TrySpawnNextAsync(row, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return MissionMapping.ToInstanceDto(row.Instance, row.Template);
    }

    public async Task<MissionInstanceDto> RejectAsync(
        Guid actorUserId,
        Guid instanceId,
        CancellationToken cancellationToken = default)
    {
        var row = await LoadInstanceWithTemplate(instanceId, cancellationToken);
        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, row.Instance.HouseholdId, cancellationToken);

        if (row.Instance.Status != MissionInstanceStatus.Submitted)
            throw new InvalidOperationException("Mission is not waiting for approval.");

        row.Instance.Status = MissionInstanceStatus.Rejected;
        await _db.SaveChangesAsync(cancellationToken);
        return MissionMapping.ToInstanceDto(row.Instance, row.Template);
    }

    private async Task TrySpawnNextAsync(InstanceTemplateRow row, CancellationToken cancellationToken)
    {
        var household = await _db.Households
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == row.Instance.HouseholdId, cancellationToken);
        if (household is null)
            return;

        await _spawn.TrySpawnNextAfterApprovalAsync(row.Template, row.Instance, household, cancellationToken);
    }

    private async Task AppendMissionLedgerAsync(
        MissionInstance instance,
        MissionTemplate template,
        CancellationToken cancellationToken)
    {
        var userId = instance.AssignedToUserId
            ?? throw new InvalidOperationException("Approved instance must have an assignee.");

        var exists = await _db.LedgerEntries.AnyAsync(e =>
            e.ReferenceId == instance.Id
            && e.ReferenceType == nameof(MissionInstance)
            && e.EntryType == LedgerEntryType.MissionReward,
            cancellationToken);
        if (exists)
            return;

        _db.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            HouseholdId = instance.HouseholdId,
            UserId = userId,
            DeltaCoin = template.RewardCoin,
            DeltaExp = template.RewardExp,
            EntryType = LedgerEntryType.MissionReward,
            ReferenceId = instance.Id,
            ReferenceType = nameof(MissionInstance),
            CreatedUtc = DateTime.UtcNow
        });
    }

    private IQueryable<InstanceTemplateRow> QueryWithTemplate() =>
        from i in _db.MissionInstances.AsNoTracking()
        join t in _db.MissionTemplates.AsNoTracking() on i.TemplateId equals t.Id
        select new InstanceTemplateRow(i, t);

    private async Task<InstanceTemplateRow> LoadInstanceWithTemplate(Guid instanceId, CancellationToken cancellationToken)
    {
        var row = await (
            from i in _db.MissionInstances
            join t in _db.MissionTemplates on i.TemplateId equals t.Id
            where i.Id == instanceId
            select new InstanceTemplateRow(i, t)
        ).FirstOrDefaultAsync(cancellationToken);

        return row ?? throw new InvalidOperationException("Mission instance not found.");
    }

    private sealed record InstanceTemplateRow(MissionInstance Instance, MissionTemplate Template);
}
