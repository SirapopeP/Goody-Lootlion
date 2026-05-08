using Lootlion.Api.Http;
using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Lootlion.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lootlion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class HouseholdsController : ControllerBase
{
    private readonly IHouseholdService _households;

    public HouseholdsController(IHouseholdService households)
    {
        _households = households;
    }

    [AllowAnonymous]
    [HttpGet("for-child-registration")]
    [ProducesResponseType(typeof(IReadOnlyList<HouseholdDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<HouseholdDto>> ListForChildRegistration(CancellationToken cancellationToken) =>
        _households.ListOpenForChildJoinAsync(cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(HouseholdDto), StatusCodes.Status200OK)]
    public Task<HouseholdDto> Create([FromBody] CreateHouseholdRequest request, CancellationToken cancellationToken) =>
        _households.CreateAsync(this.GetCurrentUserId(), request, cancellationToken);

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<HouseholdMineDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<HouseholdMineDto>> ListMine(CancellationToken cancellationToken) =>
        _households.ListMineAsync(this.GetCurrentUserId(), cancellationToken);

    [HttpGet("{householdId:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<HouseholdMemberDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<HouseholdMemberDto>> Members(Guid householdId, CancellationToken cancellationToken) =>
        _households.GetMembersAsync(this.GetCurrentUserId(), householdId, cancellationToken);

    public sealed record AddMemberBody(Guid MemberUserId, MemberRole Role);

    [HttpPost("{householdId:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddMember(Guid householdId, [FromBody] AddMemberBody body, CancellationToken cancellationToken)
    {
        await _households.AddMemberAsync(this.GetCurrentUserId(), householdId, body.MemberUserId, body.Role, cancellationToken);
        return NoContent();
    }

    [HttpPost("{householdId:guid}/members/{memberUserId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApproveMember(Guid householdId, Guid memberUserId, CancellationToken cancellationToken)
    {
        await _households.ApproveMemberAsync(this.GetCurrentUserId(), householdId, memberUserId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{householdId:guid}/members/{memberUserId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RejectMember(Guid householdId, Guid memberUserId, CancellationToken cancellationToken)
    {
        await _households.RejectMemberAsync(this.GetCurrentUserId(), householdId, memberUserId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{householdId:guid}/invite-candidates")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSearchHitDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<UserSearchHitDto>> SearchInviteCandidates(
        Guid householdId,
        [FromQuery] string? q,
        CancellationToken cancellationToken) =>
        _households.SearchInviteCandidatesAsync(this.GetCurrentUserId(), householdId, q ?? string.Empty, cancellationToken);

    public sealed record DeleteHouseholdBody(string ConfirmationName);

    [HttpPost("{householdId:guid}/delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteHousehold(Guid householdId, [FromBody] DeleteHouseholdBody body, CancellationToken cancellationToken)
    {
        await _households.DeleteHouseholdAsync(this.GetCurrentUserId(), householdId, body.ConfirmationName, cancellationToken);
        return NoContent();
    }
}
