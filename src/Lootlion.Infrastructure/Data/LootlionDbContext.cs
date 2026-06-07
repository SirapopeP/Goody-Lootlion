using Lootlion.Application.Abstractions;
using Lootlion.Domain.Entities;
using Lootlion.Domain.Enums;
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
    public DbSet<MissionTemplate> MissionTemplates => Set<MissionTemplate>();
    public DbSet<MissionInstance> MissionInstances => Set<MissionInstance>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<RewardCatalogItem> RewardCatalogItems => Set<RewardCatalogItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Redemption> Redemptions => Set<Redemption>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(e =>
        {
            e.HasIndex(x => x.GuestAccountExpiresUtc);
        });

        builder.Entity<Household>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.AllowChildPickJoin).HasDefaultValue(true);
            e.Property(x => x.TimeZoneId).HasMaxLength(64).HasDefaultValue("Asia/Bangkok");
            e.HasIndex(x => x.CreatedUtc);
        });

        builder.Entity<HouseholdMember>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<int>().HasDefaultValue(HouseholdMembershipStatus.Active);
            e.HasIndex(x => new { x.HouseholdId, x.UserId }).IsUnique();
            e.HasOne(x => x.Household)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MissionTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(512);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.HasIndex(x => new { x.HouseholdId, x.IsActive });
            e.HasOne(x => x.Household)
                .WithMany()
                .HasForeignKey(x => x.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MissionInstance>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PeriodKey).HasMaxLength(128);
            e.HasIndex(x => new { x.TemplateId, x.PeriodKey }).IsUnique();
            e.HasIndex(x => new { x.HouseholdId, x.Status, x.AssignedToUserId });
            e.HasOne(x => x.Template)
                .WithMany(x => x.Instances)
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
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
