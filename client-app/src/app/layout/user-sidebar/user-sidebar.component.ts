import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { AuthSession } from '../../auth/auth.models';

type SidebarItem = {
  label: string;
  route: string;
  icon: string;
  exact?: boolean;
};

@Component({
  selector: 'app-user-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: 'user-sidebar.component.html',
  styleUrls: ['user-sidebar.component.scss']
})
export class UserSidebarComponent {
  @Input() user: AuthSession | null = null;
  @Input() collapsed = false;
  @Output() readonly collapsedChange = new EventEmitter<boolean>();

  readonly items: SidebarItem[] = [
    { label: 'Profile Overview', route: '/profile', icon: 'fa-regular fa-user', exact: true },
    { label: 'Create Auction', route: '/create', icon: 'fa-solid fa-plus' },
    { label: 'Payment Methods', route: '/payment-methods', icon: 'fa-regular fa-credit-card' },
    { label: 'My Bids', route: '/my-bids', icon: 'fa-solid fa-gavel' },
    { label: 'Watchlist', route: '/watchlist', icon: 'fa-regular fa-heart' },
    { label: 'Settings', route: '/settings', icon: 'fa-solid fa-gear' }
  ];

  get initials(): string {
    const name = this.user?.fullName?.trim();
    if (!name) return 'US';
    const parts = name.split(/\s+/).filter(Boolean);
    return parts.length === 1
      ? parts[0].slice(0, 2).toUpperCase()
      : `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  }

  toggleCollapsed(): void {
    this.collapsed = !this.collapsed;
    this.collapsedChange.emit(this.collapsed);
  }
}
