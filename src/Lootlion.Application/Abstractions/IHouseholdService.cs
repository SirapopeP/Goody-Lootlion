using Lootlion.Application.Dtos;
using Lootlion.Domain.Enums;

namespace Lootlion.Application.Abstractions;

public interface IHouseholdService
{
    Task<HouseholdDto> CreateAsync(Guid actorUserId, CreateHouseholdRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HouseholdDto>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HouseholdDto>> ListOpenForChildJoinAsync(CancellationToken cancellationToken = default);
    Task JoinHouseholdAsParentAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HouseholdMemberDto>> GetMembersAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
    Task AddMemberAsync(Guid actorUserId, Guid householdId, Guid memberUserId, MemberRole role, CancellationToken cancellationToken = default);
}
