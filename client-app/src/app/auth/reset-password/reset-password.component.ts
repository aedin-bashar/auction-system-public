import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../auth.service';
import { ResetPasswordRequest } from '../auth.models';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: 'reset-password.component.html',
  styleUrls: ['reset-password.component.scss']
})
export class ResetPasswordComponent {
  private readonly cdr = inject(ChangeDetectorRef);

  isSubmitting = false;
  errorMessage: string | null = null;
  showNewPassword = false;
  showConfirmNewPassword = false;

  readonly form;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {
    this.form = this.formBuilder.group(
      {
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmNewPassword: ['', [Validators.required]]
      },
      { validators: [this.matchPasswords] }
    );
  }

  private get token(): string {
    return this.route.snapshot.queryParamMap.get('token') ?? '';
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.token) {
      this.errorMessage = 'The reset link is missing a token. Request a new reset link.';
      return;
    }

    this.errorMessage = null;
    this.isSubmitting = true;

    const payload: ResetPasswordRequest = {
      token: this.token,
      newPassword: this.form.get('newPassword')?.value as string,
      confirmNewPassword: this.form.get('confirmNewPassword')?.value as string
    };

    this.authService
      .resetPassword(payload)
      .pipe(
        finalize(() => {
          this.isSubmitting = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: () => {
          void this.router.navigate(['/login']);
        },
        error: (err: unknown) => {
          this.errorMessage = this.formatError(err);
        }
      });
  }

  toggleNewPasswordVisibility(): void {
    this.showNewPassword = !this.showNewPassword;
  }

  toggleConfirmNewPasswordVisibility(): void {
    this.showConfirmNewPassword = !this.showConfirmNewPassword;
  }

  private matchPasswords = (group: AbstractControl): ValidationErrors | null => {
    const password = group.get('newPassword')?.value as string | undefined;
    const confirm = group.get('confirmNewPassword')?.value as string | undefined;
    if (!password || !confirm) return null;
    return password === confirm ? null : { passwordMismatch: true };
  };

  private formatError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 400) {
        const error = err.error?.error as string | undefined;
        return error ?? 'The reset link is invalid or has expired. Request a new one.';
      }
      if (err.status === 0) {
        return 'Could not reach the server. Check your connection and try again.';
      }
    }
    return 'Something went wrong. Please try again.';
  }
}
