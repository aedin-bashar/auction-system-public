import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  booleanAttribute
} from '@angular/core';
import { FormsModule } from '@angular/forms';

export type ChangePasswordPayload = {
  currentPassword: string;
  newPassword: string;
};

@Component({
  selector: 'app-change-password-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './change-password-modal.component.html',
  styleUrl: './change-password-modal.component.scss'
})
export class ChangePasswordModalComponent implements OnChanges {
  @Input({ transform: booleanAttribute })
  open = false;

  @Output()
  openChange = new EventEmitter<boolean>();

  @Output()
  passwordChanged = new EventEmitter<ChangePasswordPayload>();

  @Input({ transform: booleanAttribute })
  busy = false;

  @Input()
  serverError: string | null = null;

  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;
  errorMessage: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']) {
      this.open ? this.lockScroll() : this.unlockScroll();
    }

    if (changes['open'] && this.open) {
      this.resetForm();
    }
  }

  onBackdropClick(): void {
    this.close();
  }

  onDialogClick(event: MouseEvent): void {
    event.stopPropagation();
  }

  cancel(): void {
    this.close();
  }

  save(): void {
    this.errorMessage = null;

    if (!this.currentPassword.trim()) {
      this.errorMessage = 'Current password is required.';
      return;
    }

    if (this.newPassword.length < 8 || this.newPassword.length > 128) {
      this.errorMessage = 'New password must be between 8 and 128 characters.';
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.errorMessage = 'New password and confirmation do not match.';
      return;
    }

    if (this.currentPassword === this.newPassword) {
      this.errorMessage = 'New password must be different from current password.';
      return;
    }

    this.passwordChanged.emit({
      currentPassword: this.currentPassword.trim(),
      newPassword: this.newPassword
    });
  }

  toggleCurrentPasswordVisibility(): void {
    this.showCurrentPassword = !this.showCurrentPassword;
  }

  toggleNewPasswordVisibility(): void {
    this.showNewPassword = !this.showNewPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.open) return;
    this.close();
  }

  private resetForm(): void {
    this.currentPassword = '';
    this.newPassword = '';
    this.confirmPassword = '';
    this.showCurrentPassword = false;
    this.showNewPassword = false;
    this.showConfirmPassword = false;
    this.errorMessage = null;
  }

  private close(): void {
    this.open = false;
    this.openChange.emit(false);
    this.unlockScroll();
    this.errorMessage = null;
  }

  private lockScroll(): void {
    if (typeof document === 'undefined') return;
    document.body.style.overflow = 'hidden';
  }

  private unlockScroll(): void {
    if (typeof document === 'undefined') return;
    document.body.style.overflow = '';
  }
}
