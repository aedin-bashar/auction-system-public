import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

export type BidRealtimeState = 'winning' | 'outbid' | null;

@Component({
  selector: 'app-bid-state-feedback',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './bid-state-feedback.component.html',
  styleUrl: './bid-state-feedback.component.scss'
})
export class BidStateFeedbackComponent {
  @Input()
  state: BidRealtimeState = null;

  @Input()
  compact = false;

  get hasState(): boolean {
    return this.state !== null;
  }

  get label(): string {
    return this.state === 'winning' ? 'You are currently winning' : 'You have been outbid';
  }
}
