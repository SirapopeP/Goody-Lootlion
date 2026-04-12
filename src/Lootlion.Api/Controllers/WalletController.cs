using Lootlion.Api.Http;
using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lootlion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class WalletController : ControllerBase
{
    private readonly IWalletService _wallet;

    public WalletController(IWalletService wallet)
    {
        _wallet = wallet;
    }

    [HttpGet("household/{householdId:guid}/balance")]
    [ProducesResponseType(typeof(WalletBalanceDto), StatusCodes.Status200OK)]
    public Task<WalletBalanceDto> Balance(Guid householdId, CancellationToken cancellationToken) =>
        _wallet.GetBalanceAsync(this.GetCurrentUserId(), householdId, cancellationToken);

    [HttpGet("household/{householdId:guid}/ledger")]
    [ProducesResponseType(typeof(IReadOnlyList<LedgerEntryDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<LedgerEntryDto>> Ledger(Guid householdId, [FromQuery] int take = 50, CancellationToken cancellationToken = default) =>
        _wallet.GetLedgerAsync(this.GetCurrentUserId(), householdId, take, cancellationToken);
}
