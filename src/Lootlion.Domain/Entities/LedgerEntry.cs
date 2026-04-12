using Lootlion.Domain.Enums;

namespace Lootlion.Domain.Entities;

public class LedgerEntry
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid UserId { get; set; }
    public long DeltaCoin { get; set; }
    public int DeltaExp { get; set; }
    public LedgerEntryType EntryType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime CreatedUtc { get; set; }
}
