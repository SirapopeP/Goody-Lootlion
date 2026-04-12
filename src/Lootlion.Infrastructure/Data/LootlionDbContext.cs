using Lootlion.Application.Abstractions;
using Lootlion.Domain.Entities;
using Lootlion.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Infrastructure.Data;

public sealed class LootlionDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>, ILootlionDbContext
{
    public LootlionDbContext(DbContextOptions<LootlionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Household> Households => Set<Household>();
    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<RewardCatalogItem> RewardCatalogItems => Set<RewardCatalogItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Redemption> Redemptions => Set<Redemption>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Household>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256);
            e.HasIndex(x => x.CreatedUtc);
        });

        builder.Entity<HouseholdMember>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.HouseholdId, x.UserId }).IsUnique();
            e.HasOne(x => x.Household)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Mission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(512);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.HasIndex(x => new { x.HouseholdId, x.Status });
            e.HasOne(x => x.Household)
                .WithMany()
                .HasForeignKey(x => x.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LedgerEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ReferenceType).HasMaxLength(64);
            e.HasIndex(x => new { x.HouseholdId, x.UserId, x.CreatedUtc });
            e.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
        });

        builder.Entity<RewardCatalogItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(512);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.HasIndex(x => x.HouseholdId);
        });

        builder.Entity<WishlistItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(512);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.HasIndex(x => x.HouseholdId);
        });

        builder.Entity<Redemption>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.HouseholdId, x.UserId, x.CreatedUtc });
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UserId);
        });
    }
}
