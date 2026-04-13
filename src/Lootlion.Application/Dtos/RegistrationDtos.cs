namespace Lootlion.Application.Dtos;

public enum RegistrationRoleDto
{
    Parent = 0,
    Child = 1
}

/// <summary>ลงทะเบียนครั้งเดียวตอนจบ wizard — ผู้ปกครองกรอก username/password; เด็ก join เป็น guest ไม่มี password ที่รู้</summary>
public sealed record RegisterWizardRequest(
    string Nickname,
    RegistrationRoleDto Role,
    bool CreateNewHousehold,
    string? NewHouseholdName,
    Guid? JoinHouseholdIdAsParent,
    string? UserName,
    string? Email,
    string? Password,
    Guid? JoinHouseholdIdAsChild);

/// <summary>ผู้ปกครองกรอกบัญชีให้เด็ก guest ให้ครบภายในกำหนด</summary>
public sealed record CompleteGuestChildRequest(
    Guid ChildUserId,
    string UserName,
    string? Email,
    string Password);
