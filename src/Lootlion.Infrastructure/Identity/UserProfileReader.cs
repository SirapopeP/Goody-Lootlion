using Lootlion.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Infrastructure.Identity;

public sealed class UserProfileReader : IUserProfileReader
{
    private readonly UserManager<AppUser> _users;

    public UserProfileReader(UserManager<AppUser> users)
    {
        _users = users;
    }

    public async Task<UserProfile?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var u = await _users.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return u is null ? null : new UserProfile(u.Id, u.Email ?? string.Empty, u.DisplayName);
    }

    public async Task<IReadOnlyDictionary<Guid, UserProfile>> GetManyAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, UserProfile>();

        var rows = await _users.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken);
        return rows.ToDictionary(u => u.Id, u => new UserProfile(u.Id, u.Email ?? string.Empty, u.DisplayName));
    }
}
