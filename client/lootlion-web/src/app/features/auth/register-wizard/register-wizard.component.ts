import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AuthService } from '../../../api/generated/api/auth.service';
import { HouseholdsService } from '../../../api/generated/api/households.service';
import { HouseholdDto } from '../../../api/generated/model/householdDto';
import { readApiErrorMessage } from '../../../core/auth/api-error';
import { AuthSessionService } from '../../../core/auth/auth-session.service';
import { LanguageToggleComponent } from '../../../core/i18n/language-toggle.component';
import { ParticlesBackgroundComponent } from '../../../shared/ui/particles-background/particles-background.component';
import { PASSWORD_MIN_LENGTH, PASSWORD_PATTERN } from '../../../core/auth/password-policy';
import { EMPTY, catchError, finalize } from 'rxjs';

const DRAFT_KEY = 'lootlion.register.wizard';

type Role = 'parent' | 'child';

interface Draft {
  nickname: string;
  role: Role | null;
  parentMode: 'create' | 'join' | null;
  newHouseholdName: string;
  joinHouseholdId: string | null;
  userName: string;
  email: string;
  password: string;
}

const emptyDraft = (): Draft => ({
  nickname: '',
  role: null,
  parentMode: null,
  newHouseholdName: '',
  joinHouseholdId: null,
  userName: '',
  email: '',
  password: '',
});

@Component({
  selector: 'app-register-wizard',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe,
    LanguageToggleComponent,
    ParticlesBackgroundComponent,
  ],
  templateUrl: './register-wizard.component.html',
  styleUrls: ['../auth-shell.css', '../auth-forms.css', './register-wizard.component.css'],
})
export class RegisterWizardComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthService);
  private readonly householdsApi = inject(HouseholdsService);
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly step = signal(0);
  readonly households = signal<HouseholdDto[]>([]);
  readonly loadingHouseholds = signal(false);

  readonly passwordMinLength = PASSWORD_MIN_LENGTH;

  draft: Draft = emptyDraft();

  readonly nicknameForm = this.fb.nonNullable.group({
    nickname: ['', Validators.required],
  });

  readonly parentAccountForm = this.fb.nonNullable.group({
    userName: ['', Validators.required],
    email: [''],
    password: [
      '',
      [Validators.required, Validators.minLength(PASSWORD_MIN_LENGTH), Validators.pattern(PASSWORD_PATTERN)],
    ],
  });

  ngOnInit(): void {
    const raw = sessionStorage.getItem(DRAFT_KEY);
    if (raw) {
      try {
        const parsed = JSON.parse(raw) as Draft;
        this.draft = { ...emptyDraft(), ...parsed };
        this.nicknameForm.patchValue({ nickname: this.draft.nickname });
        this.parentAccountForm.patchValue({
          userName: this.draft.userName,
          email: this.draft.email,
          password: this.draft.password,
        });
      } catch {
        sessionStorage.removeItem(DRAFT_KEY);
      }
    }
  }

  private persistDraft(): void {
    sessionStorage.setItem(DRAFT_KEY, JSON.stringify(this.draft));
  }

  private clearDraft(): void {
    sessionStorage.removeItem(DRAFT_KEY);
  }

  selectRole(role: Role): void {
    this.draft.role = role;
    this.persistDraft();
    if (role === 'child') {
      this.step.set(2);
      this.loadHouseholds();
    } else {
      this.draft.parentMode = null;
      this.draft.joinHouseholdId = null;
      this.draft.newHouseholdName = '';
      this.persistDraft();
      this.step.set(2);
    }
  }

  chooseParentCreate(): void {
    this.draft.parentMode = 'create';
    this.draft.joinHouseholdId = null;
    this.persistDraft();
  }

  chooseParentJoin(): void {
    this.draft.parentMode = 'join';
    this.draft.newHouseholdName = '';
    this.persistDraft();
    this.loadHouseholds();
  }

  patchDraft(partial: Partial<Draft>): void {
    this.draft = { ...this.draft, ...partial };
    this.persistDraft();
  }

  onNewHouseholdNameInput(event: Event): void {
    const v = (event.target as HTMLInputElement).value;
    this.patchDraft({ newHouseholdName: v });
  }

  selectHouseholdForParent(id: string): void {
    this.draft.joinHouseholdId = id;
    this.persistDraft();
  }

  loadHouseholds(): void {
    this.loadingHouseholds.set(true);
    this.householdsApi.apiHouseholdsForChildRegistrationGet().subscribe({
      next: (list) => {
        this.households.set(list ?? []);
        this.loadingHouseholds.set(false);
      },
      error: () => {
        this.households.set([]);
        this.loadingHouseholds.set(false);
      },
    });
  }

  nextFromNickname(): void {
    if (this.nicknameForm.invalid) {
      this.nicknameForm.markAllAsTouched();
      return;
    }
    this.draft.nickname = this.nicknameForm.controls.nickname.value.trim();
    this.persistDraft();
    this.step.set(1);
  }

  back(): void {
    const s = this.step();
    if (s === 3) {
      this.step.set(2);
      return;
    }
    if (s === 2 && this.draft.role === 'parent' && this.draft.parentMode !== null) {
      this.draft.parentMode = null;
      this.persistDraft();
      return;
    }
    if (s > 0) {
      this.step.set(s - 1);
    }
  }

  goParentCredentials(): void {
    if (this.draft.role !== 'parent') {
      return;
    }
    if (this.draft.parentMode === 'create') {
      const name = this.draft.newHouseholdName?.trim();
      if (!name) {
        this.errorMessage.set(this.transloco.translate('auth.wizard.familyNameRequired'));
        return;
      }
    }
    if (this.draft.parentMode === 'join') {
      if (!this.draft.joinHouseholdId) {
        this.errorMessage.set(this.transloco.translate('auth.wizard.pickFamily'));
        return;
      }
    }
    this.errorMessage.set(null);
    this.step.set(3);
  }

  submitChild(householdId: string): void {
    this.errorMessage.set(null);
    this.submitting.set(true);
    this.authApi
      .apiAuthRegisterWizardPost({
        nickname: this.draft.nickname,
        role: 1,
        createNewHousehold: false,
        newHouseholdName: null,
        joinHouseholdIdAsParent: null,
        userName: null,
        email: null,
        password: null,
        joinHouseholdIdAsChild: householdId,
      })
      .pipe(
        catchError((err) => {
          this.errorMessage.set(readApiErrorMessage(err, this.transloco.translate('auth.registerFailed')));
          return EMPTY;
        }),
        finalize(() => this.submitting.set(false))
      )
      .subscribe((res) => {
        this.clearDraft();
        if (this.session.storeFromAuthResponse(res)) {
          void this.router.navigateByUrl('/');
        } else {
          this.errorMessage.set(this.transloco.translate('auth.noToken'));
        }
      });
  }

  submitParent(): void {
    this.errorMessage.set(null);
    if (this.parentAccountForm.invalid) {
      this.parentAccountForm.markAllAsTouched();
      return;
    }
    const v = this.parentAccountForm.getRawValue();
    this.draft.userName = v.userName.trim();
    this.draft.email = v.email.trim();
    this.draft.password = v.password;
    this.persistDraft();

    const createNew = this.draft.parentMode === 'create';
    this.submitting.set(true);
    this.authApi
      .apiAuthRegisterWizardPost({
        nickname: this.draft.nickname,
        role: 0,
        createNewHousehold: createNew,
        newHouseholdName: createNew ? this.draft.newHouseholdName.trim() : null,
        joinHouseholdIdAsParent: !createNew && this.draft.joinHouseholdId ? this.draft.joinHouseholdId : null,
        userName: this.draft.userName,
        email: this.draft.email || null,
        password: this.draft.password,
        joinHouseholdIdAsChild: null,
      })
      .pipe(
        catchError((err) => {
          this.errorMessage.set(readApiErrorMessage(err, this.transloco.translate('auth.registerFailed')));
          return EMPTY;
        }),
        finalize(() => this.submitting.set(false))
      )
      .subscribe((res) => {
        this.clearDraft();
        if (this.session.storeFromAuthResponse(res)) {
          void this.router.navigateByUrl('/');
        } else {
          this.errorMessage.set(this.transloco.translate('auth.noToken'));
        }
      });
  }

  cancel(): void {
    this.clearDraft();
    void this.router.navigateByUrl('/auth/login');
  }
}
