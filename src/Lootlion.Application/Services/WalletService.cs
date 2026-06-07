using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Lootlion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Services;

public sealed class WalletService : IWalletService
{
    private readonly ILootlionDbContext _db;
    private readonly IUserProfileReader _users;

    public WalletService(ILootlionDbContext db, IUserProfileReader users)
    {
        _db = db;
        _users = users;
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

        return ToBalanceDto(householdId, userId, coin, exp);
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

    public async Task<IReadOnlyList<HouseholdLeaderboardEntryDto>> GetLeaderboardAsync(
        Guid userId,
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        await HouseholdAccess.EnsureMemberAsync(_db, userId, householdId, cancellationToken);

        var memberUserIds = await _db.HouseholdMembers
            .AsNoTracking()
            .Where(m => m.HouseholdId == householdId && m.Status == HouseholdMembershipStatus.Active)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        if (memberUserIds.Count == 0)
            return Array.Empty<HouseholdLeaderboardEntryDto>();

        var aggregates = await _db.LedgerEntries
            .AsNoTracking()
            .Where(e => e.HouseholdId == householdId && memberUserIds.Contains(e.UserId))
            .GroupBy(e => e.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Coin = g.Sum(e => (long?)e.DeltaCoin) ?? 0,
                Exp = g.Sum(e => (int?)e.DeltaExp) ?? 0
            })
            .ToListAsync(cancellationToken);

        var aggByUser = aggregates.ToDictionary(a => a.UserId);
        var profiles = await _users.GetManyAsync(memberUserIds, cancellationToken);

        var rows = memberUserIds
            .Select(memberId =>
            {
                aggByUser.TryGetValue(memberId, out var agg);
                var coin = agg?.Coin ?? 0;
                var exp = agg?.Exp ?? 0;
                profiles.TryGetValue(memberId, out var profile);
                var displayName = (profile?.DisplayName ?? profile?.Email ?? memberId.ToString()).Trim();
                if (string.IsNullOrEmpty(displayName))
                    displayName = memberId.ToString();
                return new
                {
                    UserId = memberId,
                    DisplayName = displayName,
                    Coin = coin,
                    Exp = exp,
                    Level = LevelCalculator.FromExp(exp)
                };
            })
            .OrderByDescending(x => x.Exp)
            .ThenByDescending(x => x.Coin)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select((x, index) => new HouseholdLeaderboardEntryDto(
                x.UserId,
                x.DisplayName,
                x.Coin,
                x.Exp,
                x.Level,
                index + 1))
            .ToList();

        return rows;
    }

    private static WalletBalanceDto ToBalanceDto(Guid householdId, Guid userId, long coin, int exp) =>
        new(
            householdId,
            userId,
            coin,
            exp,
            LevelCalculator.FromExp(exp),
            LevelCalculator.ExpInCurrentLevel(exp),
            LevelCalculator.ExpToNextLevel(exp));
}
