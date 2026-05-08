using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
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

    public async Task<IReadOnlyList<UserSearchHitDto>> SearchByDisplayNameOrEmailAsync(string query, int take, CancellationToken cancellationToken = default)
    {
        var term = query.Trim();
        if (term.Length < 2)
            return Array.Empty<UserSearchHitDto>();

        take = Math.Clamp(take, 1, 40);
        var pattern = "%" + EscapeLike(term) + "%";

        var rows = await _users.Users.AsNoTracking()
            .Where(u =>
                EF.Functions.ILike(u.DisplayName, pattern) ||
                (u.Email != null && EF.Functions.ILike(u.Email, pattern)))
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Email)
            .Take(take)
            .Select(u => new UserSearchHitDto(u.Id, u.DisplayName, u.Email ?? string.Empty))
            .ToListAsync(cancellationToken);

        return rows;
    }

    private static string EscapeLike(string value)
    {
        return value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
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
