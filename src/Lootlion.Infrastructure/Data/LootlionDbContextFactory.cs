using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lootlion.Infrastructure.Data;

public sealed class LootlionDbContextFactory : IDesignTimeDbContextFactory<LootlionDbContext>
{
    public LootlionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LOOTLION_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=lootlion;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<LootlionDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new LootlionDbContext(options);
    }
}
