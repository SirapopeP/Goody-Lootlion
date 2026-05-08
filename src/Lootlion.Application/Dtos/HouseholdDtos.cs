namespace Lootlion.Application.Dtos;

public record HouseholdDto(Guid Id, string Name, DateTime CreatedUtc);

/// <summary>รายการบ้านของผู้ใช้ — มีสถานะสมาชิกของตัวเอง (Pending/Active)</summary>
public record HouseholdMineDto(Guid Id, string Name, DateTime CreatedUtc, string MembershipStatus);

public record CreateHouseholdRequest(string Name);

public record HouseholdMemberDto(Guid UserId, string DisplayName, string Email, string Role, string MembershipStatus);
