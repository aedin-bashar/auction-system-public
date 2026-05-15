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

type ContactField = 'email' | 'phone' | 'address';

@Component({
  selector: 'app-edit-contact-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './edit-contact-modal.component.html',
  styleUrls: ['./edit-contact-modal.component.scss']
})
export class EditContactModalComponent implements OnChanges {
  @Input({ transform: booleanAttribute })
  open = false;

  @Input()
  field: ContactField = 'email';

  @Input()
  value = '';

  @Output()
  openChange = new EventEmitter<boolean>();

  @Output()
  valueSaved = new EventEmitter<string>();

  draftValue = '';
  errorMessage: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']) {
      this.open ? this.lockScroll() : this.unlockScroll();
    }

    if (changes['value'] || changes['field'] || (changes['open'] && this.open)) {
      this.draftValue = this.value;
      this.errorMessage = null;
    }
  }

  get title(): string {
    if (this.field === 'email') return 'Edit Email';
    if (this.field === 'phone') return 'Edit Phone Number';
    return 'Edit Address';
  }

  get description(): string {
    if (this.field === 'email') return 'Update the email used for account notifications.';
    if (this.field === 'phone') return 'Update your phone number used for account contact.';
    return 'Update your address information shown on your profile.';
  }

  get label(): string {
    if (this.field === 'email') return 'Email';
    if (this.field === 'phone') return 'Phone Number';
    return 'Address';
  }

  get placeholder(): string {
    if (this.field === 'email') return 'you@example.com';
    if (this.field === 'phone') return '+1 (555) 123-4567';
    return 'Street, City, State, ZIP';
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
    const trimmed = this.draftValue.trim();

    if (this.field === 'email') {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(trimmed) || trimmed.length > 320) {
        this.errorMessage = 'Enter a valid email address (max 320 characters).';
        return;
      }
    }

    if (this.field === 'phone') {
      const phoneRegex = /^[0-9+\-\s()]{7,20}$/;
      if (!phoneRegex.test(trimmed)) {
        this.errorMessage = 'Enter a valid phone number (7-20 characters).';
        return;
      }
    }

    if (this.field === 'address') {
      if (trimmed.length < 5 || trimmed.length > 200) {
        this.errorMessage = 'Address must be between 5 and 200 characters.';
        return;
      }
    }

    this.valueSaved.emit(trimmed);
    this.close();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.open) return;
    this.close();
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
