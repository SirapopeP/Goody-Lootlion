using Lootlion.Api.Http;
using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lootlion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ProfileController : ControllerBase
{
    private readonly IAuthService _auth;

    public ProfileController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public Task<AuthResponse> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken) =>
        _auth.UpdateProfileAsync(this.GetCurrentUserId(), request, cancellationToken);
}
