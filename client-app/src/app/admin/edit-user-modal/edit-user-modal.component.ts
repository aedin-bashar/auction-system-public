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

export type EditUserPayload = {
  id: string;
  fullName: string;
  email: string;
  role: 'Admin' | 'Seller' | 'Bidder';
  isActive: boolean;
};

type EditableUser = {
  id: string;
  fullName: string;
  email: string;
  role: 'Admin' | 'Seller' | 'Bidder';
  isActive: boolean;
};

@Component({
  selector: 'app-edit-user-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './edit-user-modal.component.html',
  styleUrl: './edit-user-modal.component.scss'
})
export class EditUserModalComponent implements OnChanges {
  @Input({ transform: booleanAttribute })
  open = false;

  @Input()
  user: EditableUser | null = null;

  @Output()
  openChange = new EventEmitter<boolean>();

  @Output()
  saved = new EventEmitter<EditUserPayload>();

  draftFullName = '';
  draftEmail = '';
  draftRole: 'Admin' | 'Seller' | 'Bidder' = 'Bidder';
  draftActive = true;
  errorMessage: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']) {
      this.open ? this.lockScroll() : this.unlockScroll();
    }

    if ((changes['open'] && this.open) || changes['user']) {
      this.syncDraft();
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
    if (!this.user) {
      return;
    }

    this.errorMessage = null;

    const fullName = this.draftFullName.trim();
    const email = this.draftEmail.trim();

    if (fullName.length < 2 || fullName.length > 100) {
      this.errorMessage = 'Full name must be between 2 and 100 characters.';
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) || email.length > 320) {
      this.errorMessage = 'Enter a valid email address.';
      return;
    }

    this.saved.emit({
      id: this.user.id,
      fullName,
      email: email.toLowerCase(),
      role: this.draftRole,
      isActive: this.draftActive
    });

    this.close();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.open) return;
    this.close();
  }

  private syncDraft(): void {
    this.draftFullName = this.user?.fullName ?? '';
    this.draftEmail = this.user?.email ?? '';
    this.draftRole = this.user?.role ?? 'Bidder';
    this.draftActive = this.user?.isActive ?? true;
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
