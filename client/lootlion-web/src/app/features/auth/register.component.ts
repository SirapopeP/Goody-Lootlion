import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TuiButton } from '@taiga-ui/core';
import { AuthService } from '../../api/generated/api/auth.service';
import { readApiErrorMessage } from '../../core/auth/api-error';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { PASSWORD_MIN_LENGTH, PASSWORD_PATTERN } from '../../core/auth/password-policy';
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

  readonly passwordMinLength = PASSWORD_MIN_LENGTH;

  readonly form = this.fb.nonNullable.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: [
      '',
      [Validators.required, Validators.minLength(PASSWORD_MIN_LENGTH), Validators.pattern(PASSWORD_PATTERN)],
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
        catchError((err) => {
          this.errorMessage.set(readApiErrorMessage(err, 'สมัครสมาชิกไม่สำเร็จ'));
          return EMPTY;
        }),
        finalize(() => this.submitting.set(false))
      )
      .subscribe((res) => {
        if (this.session.storeFromAuthResponse(res)) {
          void this.router.navigateByUrl('/');
        } else {
          this.errorMessage.set('ไม่ได้รับ token จากเซิร์ฟเวอร์');
        }
      });
  }
}
