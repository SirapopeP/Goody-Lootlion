namespace Lootlion.Infrastructure.Identity;

/// <summary>ค่าคงที่สำหรับบัญชีเด็กแบบ guest (ยังไม่มีอีเมลจริงจากผู้ปกครอง)</summary>
public static class GuestAccountConstants
{
    public const int ExpiryDays = 7;

    /// <summary>โดเมนอีเมลสังเคราะห์ใน DB (RequireUniqueEmail) — ไม่ส่งออกใน JWT/AuthResponse ขณะเป็น guest</summary>
    public const string SyntheticEmailDomain = "@guest.lootlion.internal";
}
