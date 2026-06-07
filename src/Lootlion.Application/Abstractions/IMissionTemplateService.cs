using Lootlion.Application.Dtos;

namespace Lootlion.Application.Abstractions;

public interface IMissionTemplateService
{
    Task<MissionTemplateDto> CreateAsync(Guid actorUserId, CreateMissionTemplateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MissionTemplateDto>> ListForHouseholdAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
    Task<MissionTemplateDto> CancelAsync(Guid actorUserId, Guid templateId, CancellationToken cancellationToken = default);
    Task<MissionInstanceDto> SpawnNextRoundAsync(Guid actorUserId, Guid templateId, CancellationToken cancellationToken = default);
}
