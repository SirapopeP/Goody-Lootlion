using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lootlion.Application.Abstractions;
using Lootlion.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lootlion.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    public const string HouseholdRoleClaim = "household_role";

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken(
        Guid userId,
        string? email,
        string displayName,
        bool isGuestChild,
        MemberRole? householdMemberRole)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("display_name", displayName)
        };
        if (!string.IsNullOrEmpty(email))
            claims.Add(new Claim(ClaimTypes.Email, email));
        if (isGuestChild)
            claims.Add(new Claim("guest_account", "true"));
        if (householdMemberRole is MemberRole.Parent)
            claims.Add(new Claim(HouseholdRoleClaim, "parent"));
        else if (householdMemberRole is MemberRole.Child)
            claims.Add(new Claim(HouseholdRoleClaim, "child"));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
