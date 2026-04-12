using Lootlion.Application.Dtos;

namespace Lootlion.Application.Abstractions;

public interface IWalletService
{
    Task<WalletBalanceDto> GetBalanceAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LedgerEntryDto>> GetLedgerAsync(Guid userId, Guid householdId, int take, CancellationToken cancellationToken = default);
}
