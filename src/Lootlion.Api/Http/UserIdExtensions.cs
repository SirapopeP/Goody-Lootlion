using System.Security.Claims;

namespace Lootlion.Api.Http;

public static class UserIdExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        if (string.IsNullOrEmpty(raw) || !Guid.TryParse(raw, out var id))
            throw new InvalidOperationException("User is not authenticated.");
        return id;
    }
}
