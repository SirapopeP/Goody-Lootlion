namespace Lootlion.Domain.Entities;

public class Household
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    public ICollection<HouseholdMember> Members { get; set; } = new List<HouseholdMember>();
}
