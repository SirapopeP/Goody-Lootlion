namespace Lootlion.Application.Dtos;

public record WalletBalanceDto(Guid HouseholdId, Guid UserId, long CoinBalance, int ExpTotal);

public record LedgerEntryDto(
    Guid Id,
    long DeltaCoin,
    int DeltaExp,
    string EntryType,
    DateTime CreatedUtc,
    Guid? ReferenceId,
    string? ReferenceType);
