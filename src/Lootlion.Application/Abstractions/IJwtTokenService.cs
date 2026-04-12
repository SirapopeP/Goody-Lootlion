namespace Lootlion.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email, string displayName);
}
