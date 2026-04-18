import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { EMPTY, catchError, finalize } from 'rxjs';
import { readApiErrorMessage } from '../core/auth/api-error';
import { AuthSessionService } from '../core/auth/auth-session.service';
import { parseJwtUserDisplay } from '../core/auth/jwt-payload';
import { ProfileApiService } from '../core/profile/profile-api.service';

@Component({
  selector: 'app-edit-profile-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, TranslocoPipe],
  templateUrl: './edit-profile-dialog.component.html',
  styleUrl: './edit-profile-dialog.component.scss',
})
export class EditProfileDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly session = inject(AuthSessionService);
  private readonly profileApi = inject(ProfileApiService);
  private readonly transloco = inject(TranslocoService);

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  /** ใช้ใน template สำหรับอีเมลอ่านอย่างเดียว */
  readonly emailPreview = computed(() => parseJwtUserDisplay(this.session.token()).email ?? '—');

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(200)]],
  });

  /** เปิด modal — ดึงชื่อจาก JWT ปัจจุบัน */
  open(): void {
    this.errorMessage.set(null);
    const d = parseJwtUserDisplay(this.session.getAccessToken());
    this.form.patchValue({
      displayName: (d.displayName ?? '').trim() || '',
    });
    this.form.markAsPristine();
    this.dialogEl().nativeElement.showModal();
  }

  close(): void {
    this.dialogEl().nativeElement.close();
  }

  onBackdropClick(event: MouseEvent): void {
    const el = this.dialogEl().nativeElement;
    if (event.target === el) {
      el.close();
    }
  }

  submit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const displayName = this.form.getRawValue().displayName.trim();
    this.submitting.set(true);
    this.profileApi
      .updateMe({ displayName })
      .pipe(
        catchError((err) => {
          this.errorMessage.set(
            readApiErrorMessage(err, this.transloco.translate('profileEdit.saveFailed'))
          );
          return EMPTY;
        }),
        finalize(() => this.submitting.set(false))
      )
      .subscribe((res) => {
        if (this.session.storeFromAuthResponse(res)) {
          this.close();
        } else {
          this.errorMessage.set(this.transloco.translate('auth.noToken'));
        }
      });
  }
}
