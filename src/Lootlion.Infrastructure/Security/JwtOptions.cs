namespace Lootlion.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    /// <summary>อายุ refresh token (วัน) — หมุนเวียนผ่าน endpoint refresh</summary>
    public int RefreshTokenDays { get; set; } = 14;
}
