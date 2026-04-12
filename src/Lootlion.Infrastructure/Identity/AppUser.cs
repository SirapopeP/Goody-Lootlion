using Microsoft.AspNetCore.Identity;

namespace Lootlion.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
