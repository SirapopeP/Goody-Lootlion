namespace Lootlion.Application.Dtos;

public record WalletBalanceDto(
    Guid HouseholdId,
    Guid UserId,
    long CoinBalance,
    int ExpTotal,
    int Level,
    int ExpInCurrentLevel,
    int ExpToNextLevel);

public record HouseholdLeaderboardEntryDto(
    Guid UserId,
    string DisplayName,
    long CoinBalance,
    int ExpTotal,
    int Level,
    int Rank);

public record LedgerEntryDto(
    Guid Id,
    long DeltaCoin,
    int DeltaExp,
    string EntryType,
    DateTime CreatedUtc,
    Guid? ReferenceId,
    string? ReferenceType);
