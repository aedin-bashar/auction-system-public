import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Input, Output, booleanAttribute } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthSession } from '../../auth/auth.models';

@Component({
  selector: 'app-user-dropdown',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: 'user-dropdown.component.html',
  styleUrls: ['user-dropdown.component.scss']
})
export class UserDropdownComponent {
  @Input() user: AuthSession | null = null;

  @Input({ transform: booleanAttribute })
  compact = false;

  @Output() readonly logout = new EventEmitter<void>();

  isOpen = false;

  get isAdmin(): boolean {
    return this.user?.role === 'Admin';
  }

  get initials(): string {
    const fullName = this.user?.fullName?.trim();
    if (!fullName) return 'US';

    const parts = fullName.split(/\s+/).filter(Boolean);
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();

    return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  }

  toggle(event: MouseEvent): void {
    event.stopPropagation();
    this.isOpen = !this.isOpen;
  }

  close(): void {
    this.isOpen = false;
  }

  onMenuClick(event: MouseEvent): void {
    event.stopPropagation();
  }

  onLogout(): void {
    this.logout.emit();
    this.close();
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.close();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.close();
  }
}
