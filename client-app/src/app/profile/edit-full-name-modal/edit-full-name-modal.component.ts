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

@Component({
  selector: 'app-edit-full-name-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './edit-full-name-modal.component.html',
  styleUrls: ['./edit-full-name-modal.component.scss']
})
export class EditFullNameModalComponent implements OnChanges {
  @Input({ transform: booleanAttribute })
  open = false;

  @Input()
  fullName = '';

  @Output()
  openChange = new EventEmitter<boolean>();

  @Output()
  fullNameSaved = new EventEmitter<string>();

  draftFullName = '';
  errorMessage: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']) {
      this.open ? this.lockScroll() : this.unlockScroll();
    }

    if (changes['fullName'] || (changes['open'] && this.open)) {
      this.draftFullName = this.fullName;
      this.errorMessage = null;
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
    const trimmed = this.draftFullName.trim();

    if (trimmed.length < 2 || trimmed.length > 100) {
      this.errorMessage = 'Full name must be between 2 and 100 characters.';
      return;
    }

    this.fullNameSaved.emit(trimmed);
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
