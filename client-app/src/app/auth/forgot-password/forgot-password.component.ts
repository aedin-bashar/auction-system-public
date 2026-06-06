import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../auth.service';
import { ForgotPasswordRequest } from '../auth.models';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: 'forgot-password.component.html',
  styleUrls: ['forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  private readonly cdr = inject(ChangeDetectorRef);

  isSubmitting = false;
  submitted = false;
  errorMessage: string | null = null;

  readonly form;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly authService: AuthService
  ) {
    this.form = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage = null;
    this.isSubmitting = true;

    const payload: ForgotPasswordRequest = {
      email: this.form.get('email')?.value as string
    };

    this.authService
      .forgotPassword(payload)
      .pipe(
        finalize(() => {
          this.isSubmitting = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: () => {
          this.submitted = true;
        },
        error: () => {
          // Always show confirmation – never reveal whether the email exists.
          this.submitted = true;
        }
      });
  }
}
