using Lootlion.Api.Http;
using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lootlion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class MissionsController : ControllerBase
{
    private readonly IMissionService _missions;

    public MissionsController(IMissionService missions)
    {
        _missions = missions;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MissionDto), StatusCodes.Status200OK)]
    public Task<MissionDto> Create([FromBody] CreateMissionRequest request, CancellationToken cancellationToken) =>
        _missions.CreateAsync(this.GetCurrentUserId(), request, cancellationToken);

    [HttpGet("household/{householdId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<MissionDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<MissionDto>> List(Guid householdId, CancellationToken cancellationToken) =>
        _missions.ListForHouseholdAsync(this.GetCurrentUserId(), householdId, cancellationToken);

    [HttpPost("{missionId:guid}/submit")]
    [ProducesResponseType(typeof(MissionDto), StatusCodes.Status200OK)]
    public Task<MissionDto> Submit(Guid missionId, CancellationToken cancellationToken) =>
        _missions.SubmitAsync(this.GetCurrentUserId(), missionId, cancellationToken);

    [HttpPost("{missionId:guid}/approve")]
    [ProducesResponseType(typeof(MissionDto), StatusCodes.Status200OK)]
    public Task<MissionDto> Approve(Guid missionId, CancellationToken cancellationToken) =>
        _missions.ApproveAsync(this.GetCurrentUserId(), missionId, cancellationToken);

    [HttpPost("{missionId:guid}/reject")]
    [ProducesResponseType(typeof(MissionDto), StatusCodes.Status200OK)]
    public Task<MissionDto> Reject(Guid missionId, CancellationToken cancellationToken) =>
        _missions.RejectAsync(this.GetCurrentUserId(), missionId, cancellationToken);
}
