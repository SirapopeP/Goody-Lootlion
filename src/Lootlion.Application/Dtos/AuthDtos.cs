namespace Lootlion.Application.Dtos;

public record RegisterRequest(string Email, string Password, string DisplayName);

/// <summary>อีเมลหรือ username สำหรับล็อกอิน</summary>
public record LoginRequest(string LoginIdentifier, string Password);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Email,
    string DisplayName,
    bool IsGuestChild);
