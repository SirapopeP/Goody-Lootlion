namespace Lootlion.Domain.Entities;

public class Household
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    /// <summary>เปิดให้เด็กเลือกเข้าร่วมจากหน้าลงทะเบียน (รายการครอบครัว)</summary>
    public bool AllowChildPickJoin { get; set; }

    public ICollection<HouseholdMember> Members { get; set; } = new List<HouseholdMember>();
}
