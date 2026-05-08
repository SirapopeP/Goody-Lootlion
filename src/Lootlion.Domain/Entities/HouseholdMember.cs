using Lootlion.Domain.Enums;

namespace Lootlion.Domain.Entities;

public class HouseholdMember
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid UserId { get; set; }
    public MemberRole Role { get; set; }

    /// <summary>สมัคร/ขอเข้าร่วม = Pending — หลังผู้ปกครองอนุมัติหรือเชิญโดยตรง = Active</summary>
    public HouseholdMembershipStatus Status { get; set; } = HouseholdMembershipStatus.Active;

    public DateTime JoinedUtc { get; set; }

    public Household Household { get; set; } = null!;
}
