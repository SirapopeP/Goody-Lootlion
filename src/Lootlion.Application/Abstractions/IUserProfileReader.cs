namespace Lootlion.Application.Abstractions;

public record UserProfile(Guid UserId, string Email, string DisplayName);

public interface IUserProfileReader
{
    Task<UserProfile?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, UserProfile>> GetManyAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
