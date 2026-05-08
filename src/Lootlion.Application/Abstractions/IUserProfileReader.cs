using Lootlion.Application.Dtos;

namespace Lootlion.Application.Abstractions;

public record UserProfile(Guid UserId, string Email, string DisplayName);

public interface IUserProfileReader
{
    Task<UserProfile?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, UserProfile>> GetManyAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>ค้นหาจากชื่อที่แสดงหรืออีเมล (ILIKE) — ใช้สำหรับเชิญสมาชิก</summary>
    Task<IReadOnlyList<UserSearchHitDto>> SearchByDisplayNameOrEmailAsync(string query, int take, CancellationToken cancellationToken = default);
}
