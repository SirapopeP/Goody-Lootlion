using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Services;

public sealed class WalletService : IWalletService
{
    private readonly ILootlionDbContext _db;

    public WalletService(ILootlionDbContext db)
    {
        _db = db;
    }

    public async Task<WalletBalanceDto> GetBalanceAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureMemberAsync(_db, userId, householdId, cancellationToken);

        var agg = await _db.LedgerEntries
            .AsNoTracking()
            .Where(e => e.HouseholdId == householdId && e.UserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Coin = g.Sum(e => (long?)e.DeltaCoin) ?? 0,
                Exp = g.Sum(e => (int?)e.DeltaExp) ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        var coin = agg?.Coin ?? 0;
        var exp = agg?.Exp ?? 0;

        return new WalletBalanceDto(householdId, userId, coin, exp);
    }

    public async Task<IReadOnlyList<LedgerEntryDto>> GetLedgerAsync(Guid userId, Guid householdId, int take, CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureMemberAsync(_db, userId, householdId, cancellationToken);

        var rows = await _db.LedgerEntries
            .AsNoTracking()
            .Where(e => e.HouseholdId == householdId && e.UserId == userId)
            .OrderByDescending(e => e.CreatedUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows
            .Select(e => new LedgerEntryDto(
                e.Id,
                e.DeltaCoin,
                e.DeltaExp,
                e.EntryType.ToString(),
                e.CreatedUtc,
                e.ReferenceId,
                e.ReferenceType))
            .ToList();
    }
}
