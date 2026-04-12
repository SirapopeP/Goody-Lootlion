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

    [HttpPost]
    [ProducesResponseType(typeof(HouseholdDto), StatusCodes.Status200OK)]
    public Task<HouseholdDto> Create([FromBody] CreateHouseholdRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return _households.CreateAsync(userId, request, cancellationToken);
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<HouseholdDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<HouseholdDto>> ListMine(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return _households.ListMineAsync(userId, cancellationToken);
    }

    [HttpGet("{householdId:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<HouseholdMemberDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<HouseholdMemberDto>> Members(Guid householdId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return _households.GetMembersAsync(userId, householdId, cancellationToken);
    }

    public sealed record AddMemberBody(Guid MemberUserId, MemberRole Role);

    [HttpPost("{householdId:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddMember(Guid householdId, [FromBody] AddMemberBody body, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await _households.AddMemberAsync(userId, householdId, body.MemberUserId, body.Role, cancellationToken);
        return NoContent();
    }
}
