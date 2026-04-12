using Lootlion.Application.Dtos;

namespace Lootlion.Application.Abstractions;

public interface IMissionService
{
    Task<MissionDto> CreateAsync(Guid actorUserId, CreateMissionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MissionDto>> ListForHouseholdAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
    Task<MissionDto> SubmitAsync(Guid actorUserId, Guid missionId, CancellationToken cancellationToken = default);
    Task<MissionDto> ApproveAsync(Guid actorUserId, Guid missionId, CancellationToken cancellationToken = default);
    Task<MissionDto> RejectAsync(Guid actorUserId, Guid missionId, CancellationToken cancellationToken = default);
}
