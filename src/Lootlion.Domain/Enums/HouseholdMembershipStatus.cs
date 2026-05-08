namespace Lootlion.Domain.Enums;

/// <summary>สถานะการเป็นสมาชิกในครอบครัว — การสมัครเข้าร่วมแบบเลือกบ้านจะเป็น Pending จนกว่าผู้ปกครองที่ Active จะอนุมัติ</summary>
public enum HouseholdMembershipStatus
{
    Pending = 0,
    Active = 1
}
