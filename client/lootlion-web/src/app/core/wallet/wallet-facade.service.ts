import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, Subject, catchError, finalize, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ActiveHouseholdService } from '../household/active-household.service';
import {
  HouseholdLeaderboardEntryDto,
  LedgerEntryDto,
  WalletBalanceDto,
} from './wallet.models';

@Injectable({ providedIn: 'root' })
export class WalletFacadeService {
  private readonly http = inject(HttpClient);
  private readonly activeHousehold = inject(ActiveHouseholdService);
  private readonly base = `${environment.apiBaseUrl}/api/Wallet`;

  readonly balance = signal<WalletBalanceDto | null>(null);
  readonly leaderboard = signal<HouseholdLeaderboardEntryDto[]>([]);
  readonly ledger = signal<LedgerEntryDto[]>([]);
  readonly balanceLoading = signal(false);
  readonly leaderboardLoading = signal(false);
  readonly ledgerLoading = signal(false);

  private readonly refresh$ = new Subject<string>();

  constructor() {
    this.refresh$.subscribe((householdId) => this.loadAll(householdId));
    this.activeHousehold.sidebarRefresh$.subscribe(() => {
      const hid = this.activeHousehold.activeHouseholdId();
      if (hid) {
        this.loadAll(hid);
      }
    });
  }

  requestRefresh(householdId?: string): void {
    const hid = householdId ?? this.activeHousehold.activeHouseholdId();
    if (hid) {
      this.refresh$.next(hid);
    }
  }

  levelByUserId(userId: string | undefined): number | null {
    if (!userId) {
      return null;
    }
    const row = this.leaderboard().find((x) => x.userId === userId);
    return row?.level ?? null;
  }

  loadBalance(householdId: string): Observable<WalletBalanceDto | null> {
    this.balanceLoading.set(true);
    return this.http.get<WalletBalanceDto>(`${this.base}/household/${householdId}/balance`).pipe(
      tap((row) => this.balance.set(row)),
      catchError(() => {
        this.balance.set(null);
        return of(null);
      }),
      finalize(() => this.balanceLoading.set(false))
    );
  }

  loadLeaderboard(householdId: string): Observable<HouseholdLeaderboardEntryDto[]> {
    this.leaderboardLoading.set(true);
    return this.http
      .get<HouseholdLeaderboardEntryDto[]>(`${this.base}/household/${householdId}/leaderboard`)
      .pipe(
        tap((rows) => this.leaderboard.set(rows ?? [])),
        catchError(() => {
          this.leaderboard.set([]);
          return of([] as HouseholdLeaderboardEntryDto[]);
        }),
        finalize(() => this.leaderboardLoading.set(false))
      );
  }

  loadLedger(householdId: string, take = 50): Observable<LedgerEntryDto[]> {
    this.ledgerLoading.set(true);
    return this.http
      .get<LedgerEntryDto[]>(`${this.base}/household/${householdId}/ledger`, {
        params: { take },
      })
      .pipe(
        tap((rows) => this.ledger.set(rows ?? [])),
        catchError(() => {
          this.ledger.set([]);
          return of([] as LedgerEntryDto[]);
        }),
        finalize(() => this.ledgerLoading.set(false))
      );
  }

  private loadAll(householdId: string): void {
    this.loadBalance(householdId).subscribe();
    this.loadLeaderboard(householdId).subscribe();
  }
}
