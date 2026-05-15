import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly activeRequestsSubject = new BehaviorSubject<number>(0);
  private readonly isLoadingSubject = new BehaviorSubject<boolean>(false);
  private updateQueued = false;

  readonly isLoading$: Observable<boolean> = this.isLoadingSubject.asObservable();

  start(): void {
    const nextCount = this.activeRequestsSubject.value + 1;
    this.activeRequestsSubject.next(nextCount);
    this.queueLoadingStateUpdate();
  }

  stop(): void {
    const nextCount = Math.max(0, this.activeRequestsSubject.value - 1);
    this.activeRequestsSubject.next(nextCount);
    this.queueLoadingStateUpdate();
  }

  private queueLoadingStateUpdate(): void {
    if (this.updateQueued) {
      return;
    }

    this.updateQueued = true;
    queueMicrotask(() => {
      this.updateQueued = false;
      this.isLoadingSubject.next(this.activeRequestsSubject.value > 0);
    });
  }
}
