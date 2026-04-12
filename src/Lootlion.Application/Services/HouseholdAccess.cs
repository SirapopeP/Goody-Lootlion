using Lootlion.Application.Abstractions;
using Lootlion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lootlion.Application.Services;

/// <summary>ตรวจสอบสิทธิ์สมาชิกครัวเรือนแบบรวมศูนย์ — ใช้ทุกที่ที่ต้องยืนยันก่อนเข้าถึงข้อมูลตาม household</summary>
internal static class HouseholdAccess
{
    public static async Task EnsureMemberAsync(
        ILootlionDbContext db,
        Guid userId,
        Guid householdId,
        CancellationToken cancellationToken)
    {
        var ok = await db.HouseholdMembers
            .AsNoTracking()
            .AnyAsync(m => m.HouseholdId == householdId && m.UserId == userId, cancellationToken);
        if (!ok)
            throw new InvalidOperationException("Household not found or access denied.");
    }

    public static async Task EnsureParentAsync(
        ILootlionDbContext db,
        Guid userId,
        Guid householdId,
        CancellationToken cancellationToken)
    {
        var row = await db.HouseholdMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.UserId == userId, cancellationToken);
        if (row is null || row.Role != MemberRole.Parent)
            throw new InvalidOperationException("Only a parent can perform this action.");
    }
}
