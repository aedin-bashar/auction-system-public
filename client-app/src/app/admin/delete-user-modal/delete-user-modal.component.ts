import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Input, Output, booleanAttribute } from '@angular/core';

type RemovableUser = {
  id: string;
  fullName: string;
  email: string;
  role: 'Admin' | 'Seller' | 'Bidder';
};

@Component({
  selector: 'app-delete-user-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './delete-user-modal.component.html',
  styleUrl: './delete-user-modal.component.scss'
})
export class DeleteUserModalComponent {
  @Input({ transform: booleanAttribute })
  open = false;

  @Input()
  user: RemovableUser | null = null;

  @Output()
  openChange = new EventEmitter<boolean>();

  @Output()
  confirmed = new EventEmitter<string>();

  onBackdropClick(): void {
    this.close();
  }

  onDialogClick(event: MouseEvent): void {
    event.stopPropagation();
  }

  cancel(): void {
    this.close();
  }

  delete(): void {
    if (!this.user) {
      return;
    }

    this.confirmed.emit(this.user.id);
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
  }
}
