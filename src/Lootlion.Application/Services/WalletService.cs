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
        await EnsureMemberAsync(userId, householdId, cancellationToken);

        var coin = await _db.LedgerEntries
            .AsNoTracking()
            .Where(e => e.HouseholdId == householdId && e.UserId == userId)
            .SumAsync(e => (long?)e.DeltaCoin, cancellationToken) ?? 0;

        var exp = await _db.LedgerEntries
            .AsNoTracking()
            .Where(e => e.HouseholdId == householdId && e.UserId == userId)
            .SumAsync(e => (int?)e.DeltaExp, cancellationToken) ?? 0;

        return new WalletBalanceDto(householdId, userId, coin, exp);
    }

    public async Task<IReadOnlyList<LedgerEntryDto>> GetLedgerAsync(Guid userId, Guid householdId, int take, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(userId, householdId, cancellationToken);

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

    private async Task EnsureMemberAsync(Guid userId, Guid householdId, CancellationToken cancellationToken)
    {
        var ok = await _db.HouseholdMembers
            .AsNoTracking()
            .AnyAsync(m => m.HouseholdId == householdId && m.UserId == userId, cancellationToken);
        if (!ok)
            throw new InvalidOperationException("Household not found or access denied.");
    }
}
