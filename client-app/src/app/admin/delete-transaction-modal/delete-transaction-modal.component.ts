import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Input, Output, booleanAttribute } from '@angular/core';

export type DeletableTransaction = {
  id: string;
  user: string;
  amount: string;
  status: 'Completed' | 'Pending' | 'Failed' | 'Refunded';
};

@Component({
  selector: 'app-delete-transaction-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './delete-transaction-modal.component.html',
  styleUrl: './delete-transaction-modal.component.scss'
})
export class DeleteTransactionModalComponent {
  @Input({ transform: booleanAttribute })
  open = false;

  @Input()
  transaction: DeletableTransaction | null = null;

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
    if (!this.transaction) {
      return;
    }

    this.confirmed.emit(this.transaction.id);
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
