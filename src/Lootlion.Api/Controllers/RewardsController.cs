using Lootlion.Api.Http;
using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lootlion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class RewardsController : ControllerBase
{
    private readonly IRewardService _rewards;

    public RewardsController(IRewardService rewards)
    {
        _rewards = rewards;
    }

    [HttpPost("catalog")]
    [ProducesResponseType(typeof(RewardCatalogItemDto), StatusCodes.Status200OK)]
    public Task<RewardCatalogItemDto> CreateCatalogItem([FromBody] CreateRewardRequest request, CancellationToken cancellationToken) =>
        _rewards.CreateCatalogItemAsync(this.GetCurrentUserId(), request, cancellationToken);

    [HttpGet("household/{householdId:guid}/catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<RewardCatalogItemDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<RewardCatalogItemDto>> ListCatalog(Guid householdId, CancellationToken cancellationToken) =>
        _rewards.ListCatalogAsync(this.GetCurrentUserId(), householdId, cancellationToken);

    public sealed record RedeemBody(Guid HouseholdId);

    [HttpPost("{rewardId:guid}/redeem")]
    [ProducesResponseType(typeof(RedemptionDto), StatusCodes.Status200OK)]
    public Task<RedemptionDto> Redeem(Guid rewardId, [FromBody] RedeemBody body, CancellationToken cancellationToken) =>
        _rewards.RedeemAsync(this.GetCurrentUserId(), body.HouseholdId, rewardId, cancellationToken);

    [HttpGet("household/{householdId:guid}/redemptions")]
    [ProducesResponseType(typeof(IReadOnlyList<RedemptionDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<RedemptionDto>> ListRedemptions(Guid householdId, CancellationToken cancellationToken) =>
        _rewards.ListRedemptionsAsync(this.GetCurrentUserId(), householdId, cancellationToken);
}
