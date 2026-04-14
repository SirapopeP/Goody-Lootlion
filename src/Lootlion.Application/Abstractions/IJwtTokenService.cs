using Lootlion.Domain.Enums;

namespace Lootlion.Application.Abstractions;

public interface IJwtTokenService
{
    /// <param name="householdMemberRole">จาก membership ครัวเรือน — null ถ้ายังไม่มีครัว</param>
    string CreateToken(
        Guid userId,
        string? email,
        string displayName,
        bool isGuestChild,
        MemberRole? householdMemberRole);
}
