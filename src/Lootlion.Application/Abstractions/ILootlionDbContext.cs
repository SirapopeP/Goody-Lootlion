using Lootlion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Abstractions;

public interface ILootlionDbContext
{
    DbSet<Household> Households { get; }
    DbSet<HouseholdMember> HouseholdMembers { get; }
    DbSet<Mission> Missions { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<RewardCatalogItem> RewardCatalogItems { get; }
    DbSet<WishlistItem> WishlistItems { get; }
    DbSet<Redemption> Redemptions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
