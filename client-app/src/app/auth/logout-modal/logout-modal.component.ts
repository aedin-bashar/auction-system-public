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

import { AuthService } from '../auth.service';

@Component({
  selector: 'app-logout-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: 'logout-modal.component.html',
  styleUrls: ['logout-modal.component.scss']
})
export class LogoutModalComponent implements OnChanges {
  @Input({ transform: booleanAttribute })
  open = false;

  @Output()
  openChange = new EventEmitter<boolean>();

  @Output()
  loggedOut = new EventEmitter<void>();

  constructor(private readonly authService: AuthService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']) {
      this.open ? this.lockScroll() : this.unlockScroll();
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

  confirmLogout(): void {
    this.authService.logout();
    this.loggedOut.emit();
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
