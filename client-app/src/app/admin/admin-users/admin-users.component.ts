import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, NgZone, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DeleteUserModalComponent } from '../delete-user-modal/delete-user-modal.component';
import { EditUserModalComponent, EditUserPayload } from '../edit-user-modal/edit-user-modal.component';
import { AdminUserManagementService, AdminUserRole } from '../admin-user-management.service';

type AdminUserItem = {
  id: string;
  fullName: string;
  email: string;
  role: AdminUserRole;
  isActive: boolean;
  joinedOn: string;
  phoneNumber: string | null;
};

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, EditUserModalComponent, DeleteUserModalComponent],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss'
})
export class AdminUsersComponent implements OnInit {
  private readonly adminUsers = inject(AdminUserManagementService);
  private readonly ngZone = inject(NgZone);
  private readonly cdr = inject(ChangeDetectorRef);

  users: AdminUserItem[] = [];
  isLoading = false;
  isSaving = false;
  errorMessage: string | null = null;

  nameFilter = '';
  statusFilter: 'all' | 'active' | 'inactive' = 'all';

  isEditModalOpen = false;
  isDeleteModalOpen = false;
  selectedUser: AdminUserItem | null = null;

  get filteredUsers(): AdminUserItem[] {
    const name = this.nameFilter.trim().toLowerCase();
    return this.users.filter((user) => {
      const matchesName =
        !name ||
        user.fullName.toLowerCase().includes(name) ||
        user.email.toLowerCase().includes(name);
      const matchesStatus =
        this.statusFilter === 'all' ||
        (this.statusFilter === 'active' && user.isActive) ||
        (this.statusFilter === 'inactive' && !user.isActive);
      return matchesName && matchesStatus;
    });
  }

  ngOnInit(): void {
    this.loadUsers();
  }

  openEdit(user: AdminUserItem): void {
    this.selectedUser = user;
    this.isEditModalOpen = true;
  }

  openDelete(user: AdminUserItem): void {
    this.selectedUser = user;
    this.isDeleteModalOpen = true;
  }

  onEditOpenChange(open: boolean): void {
    this.isEditModalOpen = open;
  }

  onDeleteOpenChange(open: boolean): void {
    this.isDeleteModalOpen = open;
  }

  onUserSaved(payload: EditUserPayload): void {
    this.errorMessage = null;
    this.isSaving = true;
    this.cdr.detectChanges();

    this.adminUsers
      .updateUser(payload.id, {
        email: payload.email,
        fullName: payload.fullName,
        phoneNumber: this.selectedUser?.phoneNumber ?? null,
        role: payload.role,
        isActive: payload.isActive
      })
      .subscribe({
        next: (updated) => {
          this.ngZone.run(() => {
            this.users = this.users.map((user) =>
              user.id === updated.userId ? this.mapToItem(updated) : user
            );
            this.isEditModalOpen = false;
            this.selectedUser = null;
            this.isSaving = false;
            this.cdr.detectChanges();
          });
        },
        error: () => {
          this.ngZone.run(() => {
            this.errorMessage = 'Could not update user. Please try again.';
            this.isSaving = false;
            this.cdr.detectChanges();
          });
        }
      });
  }

  onUserDeleted(userId: string): void {
    this.errorMessage = null;
    this.isSaving = true;
    this.cdr.detectChanges();

    this.adminUsers.deleteUser(userId).subscribe({
      next: () => {
        this.ngZone.run(() => {
          this.users = this.users.filter((user) => user.id !== userId);
          this.isDeleteModalOpen = false;
          this.selectedUser = null;
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.errorMessage = 'Could not delete user. Please try again.';
          this.isSaving = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  trackById(_: number, user: AdminUserItem): string {
    return user.id;
  }

  private loadUsers(): void {
    this.errorMessage = null;
    this.isLoading = true;
    this.cdr.detectChanges();

    this.adminUsers.listUsers().subscribe({
      next: (users) => {
        this.ngZone.run(() => {
          this.users = users.map((user) => this.mapToItem(user));
          this.isLoading = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.ngZone.run(() => {
          this.errorMessage = 'Could not load users. Please refresh and try again.';
          this.isLoading = false;
          this.cdr.detectChanges();
        });
      }
    });
  }

  private mapToItem(user: {
    userId: string;
    fullName: string;
    email: string;
    role: AdminUserRole;
    isActive: boolean;
    createdAtUtc: string;
    phoneNumber: string | null;
  }): AdminUserItem {
    return {
      id: user.userId,
      fullName: user.fullName,
      email: user.email,
      role: user.role,
      isActive: user.isActive,
      joinedOn: new Date(user.createdAtUtc).toLocaleDateString('en-US', {
        month: 'short',
        day: '2-digit',
        year: 'numeric'
      }),
      phoneNumber: user.phoneNumber
    };
  }
}
