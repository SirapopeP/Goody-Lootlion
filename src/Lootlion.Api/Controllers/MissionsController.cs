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
public sealed class MissionsController : ControllerBase
{
    private readonly IMissionTemplateService _templates;
    private readonly IMissionInstanceService _instances;

    public MissionsController(IMissionTemplateService templates, IMissionInstanceService instances)
    {
        _templates = templates;
        _instances = instances;
    }

    [HttpPost("templates")]
    [ProducesResponseType(typeof(MissionTemplateDto), StatusCodes.Status200OK)]
    public Task<MissionTemplateDto> CreateTemplate([FromBody] CreateMissionTemplateRequest request, CancellationToken cancellationToken) =>
        _templates.CreateAsync(this.GetCurrentUserId(), request, cancellationToken);

    [HttpGet("templates/household/{householdId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<MissionTemplateDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<MissionTemplateDto>> ListTemplates(Guid householdId, CancellationToken cancellationToken) =>
        _templates.ListForHouseholdAsync(this.GetCurrentUserId(), householdId, cancellationToken);

    [HttpPost("templates/{templateId:guid}/cancel")]
    [ProducesResponseType(typeof(MissionTemplateDto), StatusCodes.Status200OK)]
    public Task<MissionTemplateDto> CancelTemplate(Guid templateId, CancellationToken cancellationToken) =>
        _templates.CancelAsync(this.GetCurrentUserId(), templateId, cancellationToken);

    [HttpPost("templates/{templateId:guid}/spawn")]
    [ProducesResponseType(typeof(MissionInstanceDto), StatusCodes.Status200OK)]
    public Task<MissionInstanceDto> SpawnTemplate(Guid templateId, CancellationToken cancellationToken) =>
        _templates.SpawnNextRoundAsync(this.GetCurrentUserId(), templateId, cancellationToken);

    [HttpGet("board/household/{householdId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<MissionInstanceDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<MissionInstanceDto>> ListBoard(Guid householdId, CancellationToken cancellationToken) =>
        _instances.ListBoardAsync(this.GetCurrentUserId(), householdId, cancellationToken);

    [HttpGet("mine/household/{householdId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<MissionInstanceDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<MissionInstanceDto>> ListMine(
        Guid householdId,
        [FromQuery] MissionInstanceStatus? status,
        CancellationToken cancellationToken) =>
        _instances.ListMineAsync(this.GetCurrentUserId(), householdId, status, cancellationToken);

    [HttpGet("pending/household/{householdId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<MissionInstanceDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<MissionInstanceDto>> ListPending(Guid householdId, CancellationToken cancellationToken) =>
        _instances.ListPendingAsync(this.GetCurrentUserId(), householdId, cancellationToken);

    [HttpPost("instances/{instanceId:guid}/claim")]
    [ProducesResponseType(typeof(MissionInstanceDto), StatusCodes.Status200OK)]
    public Task<MissionInstanceDto> Claim(Guid instanceId, CancellationToken cancellationToken) =>
        _instances.ClaimAsync(this.GetCurrentUserId(), instanceId, cancellationToken);

    [HttpPost("instances/{instanceId:guid}/submit")]
    [ProducesResponseType(typeof(MissionInstanceDto), StatusCodes.Status200OK)]
    public Task<MissionInstanceDto> Submit(Guid instanceId, CancellationToken cancellationToken) =>
        _instances.SubmitAsync(this.GetCurrentUserId(), instanceId, cancellationToken);

    [HttpPost("instances/{instanceId:guid}/approve")]
    [ProducesResponseType(typeof(MissionInstanceDto), StatusCodes.Status200OK)]
    public Task<MissionInstanceDto> Approve(Guid instanceId, CancellationToken cancellationToken) =>
        _instances.ApproveAsync(this.GetCurrentUserId(), instanceId, cancellationToken);

    [HttpPost("instances/{instanceId:guid}/reject")]
    [ProducesResponseType(typeof(MissionInstanceDto), StatusCodes.Status200OK)]
    public Task<MissionInstanceDto> Reject(Guid instanceId, CancellationToken cancellationToken) =>
        _instances.RejectAsync(this.GetCurrentUserId(), instanceId, cancellationToken);
}
