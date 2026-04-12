namespace Lootlion.Application.Dtos;

public record RegisterRequest(string Email, string Password, string DisplayName);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, Guid UserId, string Email, string DisplayName);
