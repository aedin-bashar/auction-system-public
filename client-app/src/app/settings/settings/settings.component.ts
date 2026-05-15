import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import {
  ChangePasswordModalComponent,
  ChangePasswordPayload
} from '../change-password-modal/change-password-modal.component';
import { UserSecurityService } from '../user-security.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ChangePasswordModalComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent {
  private readonly security = inject(UserSecurityService);

  isChangePasswordModalOpen = false;
  isPasswordSaving = false;
  passwordError: string | null = null;
  lastPasswordUpdate = 'Feb 20, 2026';

  openChangePasswordModal(): void {
    this.passwordError = null;
    this.isChangePasswordModalOpen = true;
  }

  onChangePasswordModalOpenChange(open: boolean): void {
    this.isChangePasswordModalOpen = open;
    if (!open) {
      this.passwordError = null;
    }
  }

  onPasswordChanged(payload: ChangePasswordPayload): void {
    this.passwordError = null;
    this.isPasswordSaving = true;

    this.security.changePassword(payload.currentPassword, payload.newPassword).subscribe({
      next: () => {
        this.lastPasswordUpdate = 'Just now';
        this.isPasswordSaving = false;
        this.isChangePasswordModalOpen = false;
      },
      error: () => {
        this.passwordError = 'Could not update password. Verify your current password and try again.';
        this.isPasswordSaving = false;
      }
    });
  }
}
