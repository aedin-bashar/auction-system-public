import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';

import { AuthService } from '../auth.service';
import { RegisterRequest } from '../auth.models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: 'register.component.html',
  styleUrls: ['register.component.scss']
})
export class RegisterComponent {
  isSubmitting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  readonly form;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {
    this.form = this.formBuilder.group(
      {
        fullName: ['', [Validators.required]],
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required]],
        confirmPassword: ['', [Validators.required]],
        acceptTerms: [false, [Validators.requiredTrue]]
      },
      { validators: [this.matchPasswords] }
    );
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage = null;
    this.successMessage = null;
    this.isSubmitting = true;

    const payload: RegisterRequest = {
      fullName: this.form.get('fullName')?.value as string,
      email: this.form.get('email')?.value as string,
      password: this.form.get('password')?.value as string,
      phoneNumber: null
    };

    this.authService
      .register(payload)
      .pipe(
        timeout(15000),
        finalize(() => {
          this.isSubmitting = false;
        })
      )
      .subscribe({
        next: (result) => {
          this.successMessage = `Welcome, ${result.fullName}. Your account is ready.`;
          const targetRoute = result.role === 'Admin' ? '/admin/dashboard' : '/';
          void this.router.navigate([targetRoute]);
        },
        error: (err: unknown) => {
          if (err instanceof HttpErrorResponse) {
            const details = err.error?.details;
            this.errorMessage = typeof details === 'string' && details.trim().length > 0
              ? details
              : 'Registration failed. Please review your details and try again.';
            return;
          }

          this.errorMessage = 'Registration timed out or the server did not respond. Please try again.';
        }
      });
  }

  private matchPasswords = (): { passwordMismatch: true } | null => {
    const password = this.form?.get('password')?.value;
    const confirmPassword = this.form?.get('confirmPassword')?.value;

    if (!password || !confirmPassword) {
      return null;
    }

    return password === confirmPassword ? null : { passwordMismatch: true };
  };
}
