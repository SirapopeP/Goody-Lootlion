using Lootlion.Application.Dtos;
using Lootlion.Domain.Enums;

namespace Lootlion.Application.Abstractions;

public interface IHouseholdService
{
    Task<HouseholdDto> CreateAsync(Guid actorUserId, CreateHouseholdRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HouseholdMineDto>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HouseholdDto>> ListOpenForChildJoinAsync(CancellationToken cancellationToken = default);
    Task JoinHouseholdAsParentAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HouseholdMemberDto>> GetMembersAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
    Task AddMemberAsync(Guid actorUserId, Guid householdId, Guid memberUserId, MemberRole role, CancellationToken cancellationToken = default);

    Task ApproveMemberAsync(Guid actorUserId, Guid householdId, Guid memberUserId, CancellationToken cancellationToken = default);

    Task RejectMemberAsync(Guid actorUserId, Guid householdId, Guid memberUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSearchHitDto>> SearchInviteCandidatesAsync(Guid actorUserId, Guid householdId, string query, CancellationToken cancellationToken = default);

    Task DeleteHouseholdAsync(Guid actorUserId, Guid householdId, string confirmationName, CancellationToken cancellationToken = default);
}
