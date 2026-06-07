using Lootlion.Application.Dtos;
using Lootlion.Domain.Enums;

namespace Lootlion.Application.Abstractions;

public interface IMissionInstanceService
{
    Task<IReadOnlyList<MissionInstanceDto>> ListBoardAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MissionInstanceDto>> ListMineAsync(Guid userId, Guid householdId, MissionInstanceStatus? status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MissionInstanceDto>> ListPendingAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
    Task<MissionInstanceDto> ClaimAsync(Guid actorUserId, Guid instanceId, CancellationToken cancellationToken = default);
    Task<MissionInstanceDto> SubmitAsync(Guid actorUserId, Guid instanceId, CancellationToken cancellationToken = default);
    Task<MissionInstanceDto> ApproveAsync(Guid actorUserId, Guid instanceId, CancellationToken cancellationToken = default);
    Task<MissionInstanceDto> RejectAsync(Guid actorUserId, Guid instanceId, CancellationToken cancellationToken = default);
}
