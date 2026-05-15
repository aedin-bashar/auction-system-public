import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Input, Output, booleanAttribute } from '@angular/core';

type RemovableAuction = {
  id: string;
  title: string;
  seller: string;
  status: 'Active' | 'Ended' | 'Draft';
};

@Component({
  selector: 'app-delete-auction-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './delete-auction-modal.component.html',
  styleUrl: './delete-auction-modal.component.scss'
})
export class DeleteAuctionModalComponent {
  @Input({ transform: booleanAttribute })
  open = false;

  @Input()
  auction: RemovableAuction | null = null;

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
    if (!this.auction) {
      return;
    }

    this.confirmed.emit(this.auction.id);
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
