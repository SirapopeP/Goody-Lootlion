using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Lootlion.Domain.Entities;
using Lootlion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Services;

public sealed class MissionService : IMissionService
{
    private readonly ILootlionDbContext _db;

    public MissionService(ILootlionDbContext db)
    {
        _db = db;
    }

    public async Task<MissionDto> CreateAsync(Guid actorUserId, CreateMissionRequest request, CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureMemberAsync(_db, actorUserId, request.HouseholdId, cancellationToken);
        await HouseholdAccess.EnsureMemberAsync(_db, request.AssignedToUserId, request.HouseholdId, cancellationToken);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            HouseholdId = request.HouseholdId,
            AssignedByUserId = actorUserId,
            AssignedToUserId = request.AssignedToUserId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            RewardExp = request.RewardExp,
            RewardCoin = request.RewardCoin,
            RequiresApproval = request.RequiresApproval,
            Status = MissionStatus.Active,
            CreatedUtc = DateTime.UtcNow
        };
        _db.Missions.Add(mission);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(mission);
    }

    public async Task<IReadOnlyList<MissionDto>> ListForHouseholdAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureMemberAsync(_db, userId, householdId, cancellationToken);

        var rows = await _db.Missions
            .AsNoTracking()
            .Where(m => m.HouseholdId == householdId)
            .OrderByDescending(m => m.CreatedUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<MissionDto> SubmitAsync(Guid actorUserId, Guid missionId, CancellationToken cancellationToken = default)
    {
        var mission = await _db.Missions.FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken)
            ?? throw new InvalidOperationException("Mission not found.");

        await HouseholdAccess.EnsureMemberAsync(_db, actorUserId, mission.HouseholdId, cancellationToken);

        if (mission.AssignedToUserId != actorUserId)
            throw new InvalidOperationException("Only the assignee can submit this mission.");

        if (mission.Status != MissionStatus.Active)
            throw new InvalidOperationException("Mission cannot be submitted in its current state.");

        var now = DateTime.UtcNow;
        mission.SubmittedUtc = now;

        if (!mission.RequiresApproval)
        {
            mission.Status = MissionStatus.Approved;
            mission.CompletedUtc = now;
            await AppendMissionLedgerAsync(mission, cancellationToken);
        }
        else
        {
            mission.Status = MissionStatus.Submitted;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(mission);
    }

    public async Task<MissionDto> ApproveAsync(Guid actorUserId, Guid missionId, CancellationToken cancellationToken = default)
    {
        var mission = await _db.Missions.FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken)
            ?? throw new InvalidOperationException("Mission not found.");

        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, mission.HouseholdId, cancellationToken);

        if (mission.Status != MissionStatus.Submitted)
            throw new InvalidOperationException("Mission is not waiting for approval.");

        mission.Status = MissionStatus.Approved;
        mission.CompletedUtc = DateTime.UtcNow;
        await AppendMissionLedgerAsync(mission, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return Map(mission);
    }

    public async Task<MissionDto> RejectAsync(Guid actorUserId, Guid missionId, CancellationToken cancellationToken = default)
    {
        var mission = await _db.Missions.FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken)
            ?? throw new InvalidOperationException("Mission not found.");

        await HouseholdAccess.EnsureParentAsync(_db, actorUserId, mission.HouseholdId, cancellationToken);

        if (mission.Status != MissionStatus.Submitted)
            throw new InvalidOperationException("Mission is not waiting for approval.");

        mission.Status = MissionStatus.Rejected;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(mission);
    }

    private async Task AppendMissionLedgerAsync(Mission mission, CancellationToken cancellationToken)
    {
        var exists = await _db.LedgerEntries.AnyAsync(e =>
            e.ReferenceId == mission.Id && e.ReferenceType == nameof(Mission) && e.EntryType == LedgerEntryType.MissionReward,
            cancellationToken);
        if (exists)
            return;

        _db.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            HouseholdId = mission.HouseholdId,
            UserId = mission.AssignedToUserId,
            DeltaCoin = mission.RewardCoin,
            DeltaExp = mission.RewardExp,
            EntryType = LedgerEntryType.MissionReward,
            ReferenceId = mission.Id,
            ReferenceType = nameof(Mission),
            CreatedUtc = DateTime.UtcNow
        });
    }

    private static MissionDto Map(Mission m) =>
        new(
            m.Id,
            m.HouseholdId,
            m.AssignedByUserId,
            m.AssignedToUserId,
            m.Title,
            m.Description,
            m.RewardExp,
            m.RewardCoin,
            m.RequiresApproval,
            m.Status,
            m.CreatedUtc,
            m.SubmittedUtc,
            m.CompletedUtc);

}
