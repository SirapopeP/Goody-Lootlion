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
    public Task<WishlistItemDto> Create([FromBody] CreateWishlistRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return _wishlist.CreateAsync(userId, request, cancellationToken);
    }

    [HttpGet("household/{householdId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<WishlistItemDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<WishlistItemDto>> List(Guid householdId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return _wishlist.ListAsync(userId, householdId, cancellationToken);
    }

    [HttpPost("{wishlistItemId:guid}/approve")]
    [ProducesResponseType(typeof(RewardCatalogItemDto), StatusCodes.Status200OK)]
    public Task<RewardCatalogItemDto> Approve(Guid wishlistItemId, [FromBody] ApproveWishlistRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return _wishlist.ApproveAsync(userId, wishlistItemId, request, cancellationToken);
    }

    [HttpPost("{wishlistItemId:guid}/reject")]
    [ProducesResponseType(typeof(WishlistItemDto), StatusCodes.Status200OK)]
    public Task<WishlistItemDto> Reject(Guid wishlistItemId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return _wishlist.RejectAsync(userId, wishlistItemId, cancellationToken);
    }
}
