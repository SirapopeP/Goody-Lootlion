using Lootlion.Domain.Entities;

namespace Lootlion.Application.Abstractions;

public interface IMissionSpawnService
{
    Task<MissionInstance> SpawnInitialAsync(MissionTemplate template, Household household, CancellationToken cancellationToken = default);
    Task<MissionInstance?> TrySpawnNextAfterApprovalAsync(MissionTemplate template, MissionInstance approvedInstance, Household household, CancellationToken cancellationToken = default);
    Task<MissionInstance> SpawnManualAsync(MissionTemplate template, Household household, CancellationToken cancellationToken = default);
}
