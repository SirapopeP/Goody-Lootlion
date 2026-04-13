using Microsoft.AspNetCore.Identity;

namespace Lootlion.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>ถ้ามีค่า = บัญชีเด็กแบบ guest ยังไม่ถูกผู้ปกครองกรอก username/password ครบ — หมดอายุแล้วลบบัญชี</summary>
    public DateTime? GuestAccountExpiresUtc { get; set; }
}
