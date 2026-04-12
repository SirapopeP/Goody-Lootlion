using Lootlion.Application.Abstractions;
using Lootlion.Application.Services;
using Lootlion.Infrastructure.Data;
using Lootlion.Infrastructure.Identity;
using Lootlion.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lootlion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLootlionInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<LootlionDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<LootlionDbContext>()
            .AddSignInManager();

        services.AddScoped<ILootlionDbContext>(sp => sp.GetRequiredService<LootlionDbContext>());
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserProfileReader, UserProfileReader>();
        services.AddScoped<IAuthService, IdentityAuthService>();

        services.AddScoped<IHouseholdService, HouseholdService>();
        services.AddScoped<IMissionService, MissionService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IRewardService, RewardService>();
        services.AddScoped<IWishlistService, WishlistService>();

        return services;
    }
}
