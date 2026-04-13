import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../api/generated/api/auth.service';
import { readApiErrorMessage } from '../../core/auth/api-error';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { EMPTY, catchError, finalize } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrls: ['./auth-shell.css', './auth-forms.css'],
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthService);
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  submit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { email, password } = this.form.getRawValue();
    this.submitting.set(true);
    this.authApi
      .apiAuthLoginPost({ email: email.trim(), password })
      .pipe(
        catchError((err) => {
          this.errorMessage.set(readApiErrorMessage(err, 'เข้าสู่ระบบไม่สำเร็จ'));
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
