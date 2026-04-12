using Lootlion.Api.Http;
using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lootlion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlist;

    public WishlistController(IWishlistService wishlist)
    {
        _wishlist = wishlist;
    }

    [HttpPost]
    [ProducesResponseType(typeof(WishlistItemDto), StatusCodes.Status200OK)]
    public Task<WishlistItemDto> Create([FromBody] CreateWishlistRequest request, CancellationToken cancellationToken) =>
        _wishlist.CreateAsync(this.GetCurrentUserId(), request, cancellationToken);

    [HttpGet("household/{householdId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<WishlistItemDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<WishlistItemDto>> List(Guid householdId, CancellationToken cancellationToken) =>
        _wishlist.ListAsync(this.GetCurrentUserId(), householdId, cancellationToken);

    [HttpPost("{wishlistItemId:guid}/approve")]
    [ProducesResponseType(typeof(RewardCatalogItemDto), StatusCodes.Status200OK)]
    public Task<RewardCatalogItemDto> Approve(Guid wishlistItemId, [FromBody] ApproveWishlistRequest request, CancellationToken cancellationToken) =>
        _wishlist.ApproveAsync(this.GetCurrentUserId(), wishlistItemId, request, cancellationToken);

    [HttpPost("{wishlistItemId:guid}/reject")]
    [ProducesResponseType(typeof(WishlistItemDto), StatusCodes.Status200OK)]
    public Task<WishlistItemDto> Reject(Guid wishlistItemId, CancellationToken cancellationToken) =>
        _wishlist.RejectAsync(this.GetCurrentUserId(), wishlistItemId, cancellationToken);
}
