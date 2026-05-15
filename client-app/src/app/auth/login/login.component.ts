import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../auth.service';
import { LoginRequest } from '../auth.models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: 'login.component.html',
  styleUrls: ['login.component.scss']
})
export class LoginComponent {
  private readonly cdr = inject(ChangeDetectorRef);

  isSubmitting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  readonly form;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {
    this.form = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage = null;
    this.successMessage = null;
    this.isSubmitting = true;

    const payload = this.form.getRawValue() as LoginRequest;

    try {
      const result = await firstValueFrom(this.authService.login(payload));
      this.successMessage = `Welcome back, ${result.fullName}.`;
      const targetRoute = result.role === 'Admin' ? '/admin/dashboard' : '/';
      void this.router.navigate([targetRoute]);
    } catch (err) {
      const details = err instanceof HttpErrorResponse ? err.error?.details : undefined;
      this.errorMessage = typeof details === 'string' && details.trim().length > 0
        ? details
        : 'Login failed. Check your credentials and try again.';
    } finally {
      this.isSubmitting = false;
      this.cdr.markForCheck();
    }
  }
}
