import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TuiButton } from '@taiga-ui/core';
import { AuthService } from '../../api/generated/api/auth.service';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { EMPTY, catchError, finalize } from 'rxjs';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TuiButton],
  templateUrl: './register.component.html',
  styleUrl: './auth-forms.css',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthService);
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  /** สอดคล้องกับ ASP.NET Identity ใน API: อย่างน้อย 8 ตัว + ตัวเล็ก ใหญ่ ตัวเลข อักขระพิเศษ */
  private static readonly passwordPattern =
    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/;

  readonly form = this.fb.nonNullable.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: [
      '',
      [Validators.required, Validators.minLength(8), Validators.pattern(RegisterComponent.passwordPattern)],
    ],
  });

  submit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { displayName, email, password } = this.form.getRawValue();
    this.submitting.set(true);
    this.authApi
      .apiAuthRegisterPost({
        displayName: displayName.trim(),
        email: email.trim(),
        password,
      })
      .pipe(
        catchError((err: HttpErrorResponse) => {
          const body = err.error as { error?: string } | undefined;
          this.errorMessage.set(
            typeof body?.error === 'string' ? body.error : err.message || 'สมัครสมาชิกไม่สำเร็จ'
          );
          return EMPTY;
        }),
        finalize(() => this.submitting.set(false))
      )
      .subscribe((res) => {
        const access = res.accessToken;
        const refresh = res.refreshToken;
        if (access && refresh) {
          this.session.setSession(access, refresh);
          void this.router.navigateByUrl('/');
        } else {
          this.errorMessage.set('ไม่ได้รับ token จากเซิร์ฟเวอร์');
        }
      });
  }
}
