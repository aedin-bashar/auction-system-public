import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { EditFullNameModalComponent } from '../edit-full-name-modal/edit-full-name-modal.component';
import { EditContactModalComponent } from '../edit-contact-modal/edit-contact-modal.component';
import { ProfileService, UserProfileDto } from '../profile.service';

type ContactField = 'email' | 'phone' | 'address';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, EditFullNameModalComponent, EditContactModalComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  private readonly profileService = inject(ProfileService);
  private readonly zone = inject(NgZone);
  private readonly cdr = inject(ChangeDetectorRef);

  isEditFullNameModalOpen = false;
  isEditContactModalOpen = false;
  activeContactField: ContactField = 'email';
  isLoading = true;
  isSaving = false;
  errorMessage = '';

  readonly profile: {
    fullName: string;
    email: string;
    phoneNumber: string;
    role: string;
    address: string;
    joinedOn: string;
  } = {
    fullName: 'John Doe',
    email: 'john.doe@example.com',
    phoneNumber: '+1 (555) 123-4567',
    role: 'Bidder',
    address: '221B Baker Street, London',
    joinedOn: 'Jan 12, 2026'
  };

  ngOnInit(): void {
    this.loadProfile();
  }

  openEditFullNameModal(): void {
    this.isEditFullNameModalOpen = true;
  }

  onEditFullNameModalOpenChange(open: boolean): void {
    this.isEditFullNameModalOpen = open;
  }

  onFullNameSaved(fullName: string): void {
    const previousValue = this.profile.fullName;
    this.profile.fullName = fullName;
    this.saveProfile(
      () => {
        this.profile.fullName = previousValue;
      }
    );
  }

  openEditContactModal(field: ContactField): void {
    this.activeContactField = field;
    this.isEditContactModalOpen = true;
  }

  onEditContactModalOpenChange(open: boolean): void {
    this.isEditContactModalOpen = open;
  }

  get activeContactValue(): string {
    if (this.activeContactField === 'email') return this.profile.email;
    if (this.activeContactField === 'phone') return this.profile.phoneNumber;
    return this.profile.address;
  }

  onContactValueSaved(value: string): void {
    if (this.activeContactField === 'email') {
      const previousValue = this.profile.email;
      this.profile.email = value;
      this.saveProfile(
        () => {
          this.profile.email = previousValue;
        }
      );
      return;
    }

    if (this.activeContactField === 'phone') {
      const previousValue = this.profile.phoneNumber;
      this.profile.phoneNumber = value;
      this.saveProfile(
        () => {
          this.profile.phoneNumber = previousValue;
        }
      );
      return;
    }

    this.profile.address = value;
  }

  private loadProfile(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.profileService.getProfile().subscribe({
      next: (result) => {
        this.zone.run(() => {
          this.applyProfile(result);
          this.isLoading = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => {
          this.errorMessage = 'Could not load profile right now. Please refresh and try again.';
          this.isLoading = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  private saveProfile(rollback: () => void): void {
    this.isSaving = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.profileService.updateProfile({
      email: this.profile.email,
      fullName: this.profile.fullName,
      phoneNumber: this.profile.phoneNumber?.trim() ? this.profile.phoneNumber : null
    }).subscribe({
      next: (result) => {
        this.zone.run(() => {
          this.applyProfile(result);
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.zone.run(() => {
          rollback();
          this.errorMessage = 'Could not save profile changes. Please try again.';
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  private applyProfile(result: UserProfileDto): void {
    this.profile.fullName = result.fullName;
    this.profile.email = result.email;
    this.profile.phoneNumber = result.phoneNumber ?? 'Not set';
    this.profile.role = result.role;
    this.profile.joinedOn = new Date(result.createdAtUtc).toLocaleDateString('en-US', {
      month: 'short',
      day: '2-digit',
      year: 'numeric'
    });
  }
}
