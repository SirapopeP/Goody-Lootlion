export interface WalletBalanceDto {
  householdId?: string;
  userId?: string;
  coinBalance?: number;
  expTotal?: number;
  level?: number;
  expInCurrentLevel?: number;
  expToNextLevel?: number;
}

export interface HouseholdLeaderboardEntryDto {
  userId?: string;
  displayName?: string;
  coinBalance?: number;
  expTotal?: number;
  level?: number;
  rank?: number;
}

export interface LedgerEntryDto {
  id?: string;
  deltaCoin?: number;
  deltaExp?: number;
  entryType?: string;
  createdUtc?: string;
  referenceId?: string | null;
  referenceType?: string | null;
}
