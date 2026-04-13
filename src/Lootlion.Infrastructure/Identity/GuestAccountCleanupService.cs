using Lootlion.Application.Abstractions;
using Lootlion.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Infrastructure.Identity;

public sealed class GuestAccountCleanupService : IGuestAccountCleanupService
{
    private readonly UserManager<AppUser> _users;
    private readonly LootlionDbContext _db;

    public GuestAccountCleanupService(UserManager<AppUser> users, LootlionDbContext db)
    {
        _users = users;
        _db = db;
    }

    public async Task RunCleanupAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredIds = await _users.Users
            .AsNoTracking()
            .Where(u => u.GuestAccountExpiresUtc != null && u.GuestAccountExpiresUtc < now)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var userId in expiredIds)
        {
            var user = await _users.FindByIdAsync(userId.ToString());
            if (user is null)
                continue;

            var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(cancellationToken);
            _db.RefreshTokens.RemoveRange(tokens);

            var members = await _db.HouseholdMembers.Where(m => m.UserId == userId).ToListAsync(cancellationToken);
            _db.HouseholdMembers.RemoveRange(members);

            await _db.SaveChangesAsync(cancellationToken);
            await _users.DeleteAsync(user);
        }
    }
}
