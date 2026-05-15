import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AdminFlaggedCaseDto, AdminModerationService } from '../admin-moderation.service';

@Component({
  selector: 'app-admin-flagged-cases',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-flagged-cases.component.html',
  styleUrl: './admin-flagged-cases.component.scss'
})
export class AdminFlaggedCasesComponent implements OnInit {
  private readonly moderationApi = inject(AdminModerationService);
  private readonly ngZone = inject(NgZone);
  private readonly cdr = inject(ChangeDetectorRef);

  includeResolved = false;
  cases: AdminFlaggedCaseDto[] = [];
  selectedCaseId: string | null = null;
  selectedCase: AdminFlaggedCaseDto | null = null;
  resolutionNote = '';

  isLoading = false;
  isSaving = false;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.loadCases();
  }

  get openCount(): number {
    return this.cases.filter((item) => item.status === 'Open').length;
  }

  get resolvedCount(): number {
    return this.cases.filter((item) => item.status === 'Resolved').length;
  }

  onIncludeResolvedChanged(): void {
    this.loadCases();
  }

  selectCase(caseId: string): void {
    this.selectedCaseId = caseId;
    this.selectedCase = this.cases.find((item) => item.caseId === caseId) ?? null;
    this.resolutionNote = this.selectedCase?.resolutionNote ?? '';
  }

  resolveSelected(): void {
    const selected = this.selectedCase;
    if (!selected || selected.status === 'Resolved' || this.isSaving) {
      return;
    }

    this.errorMessage = null;
    this.isSaving = true;
    this.cdr.detectChanges();

    this.moderationApi.resolveCase(selected.caseId, this.resolutionNote.trim()).subscribe({
      next: (resolvedCase) => {
        this.ngZone.run(() => {
          if (this.includeResolved) {
            this.cases = this.cases.map((item) => item.caseId === resolvedCase.caseId ? resolvedCase : item);
            this.selectedCase = resolvedCase;
            this.selectedCaseId = resolvedCase.caseId;
          } else {
            this.cases = this.cases.filter((item) => item.caseId !== resolvedCase.caseId);
            this.selectedCaseId = this.cases[0]?.caseId ?? null;
            this.selectedCase = this.selectedCaseId
              ? this.cases.find((item) => item.caseId === this.selectedCaseId) ?? null
              : null;
          }

          this.resolutionNote = this.selectedCase?.resolutionNote ?? '';
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.errorMessage = 'Could not resolve the flagged case. Please try again.';
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  trackByCaseId(_: number, item: AdminFlaggedCaseDto): string {
    return item.caseId;
  }

  asDate(value: string | null): string {
    if (!value) {
      return '-';
    }

    return new Date(value).toLocaleString('en-US', {
      month: 'short',
      day: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  private loadCases(): void {
    this.errorMessage = null;
    this.isLoading = true;
    this.cdr.detectChanges();

    this.moderationApi.listCases(this.includeResolved).subscribe({
      next: (cases) => {
        this.ngZone.run(() => {
          this.cases = cases;
          this.selectedCaseId = cases[0]?.caseId ?? null;
          this.selectedCase = this.selectedCaseId
            ? cases.find((item) => item.caseId === this.selectedCaseId) ?? null
            : null;
          this.resolutionNote = this.selectedCase?.resolutionNote ?? '';
          this.isLoading = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.cases = [];
          this.selectedCaseId = null;
          this.selectedCase = null;
          this.resolutionNote = '';
          this.isLoading = false;
          this.errorMessage = 'Could not load flagged cases. Please refresh and try again.';
          this.cdr.detectChanges();
        });
      }
    });
  }
}