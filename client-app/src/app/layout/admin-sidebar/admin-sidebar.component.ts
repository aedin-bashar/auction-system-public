import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

type AdminNavItem = {
  label: string;
  route: string;
  icon: string;
};

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: 'admin-sidebar.component.html',
  styleUrls: ['admin-sidebar.component.scss']
})
export class AdminSidebarComponent {
  @Input() adminName = 'System Administrator';
  @Input() adminEmail = 'admin@bashar.se';
  @Input() collapsed = false;
  @Output() readonly collapsedChange = new EventEmitter<boolean>();

  readonly items: AdminNavItem[] = [
    { label: 'Dashboard', route: '/admin/dashboard', icon: 'fa-solid fa-chart-line' },
    { label: 'Create Auction', route: '/create', icon: 'fa-solid fa-plus' },
    { label: 'Manage Users', route: '/admin/users', icon: 'fa-solid fa-users' },
    { label: 'Manage Auctions', route: '/admin/auctions', icon: 'fa-solid fa-gavel' },
    { label: 'Flagged Cases', route: '/admin/flagged-cases', icon: 'fa-solid fa-flag' },
    { label: 'Transactions', route: '/admin/transactions', icon: 'fa-solid fa-money-bill-transfer' },
    { label: 'Reports', route: '/admin/reports', icon: 'fa-solid fa-chart-pie' },
    { label: 'System Settings', route: '/admin/settings', icon: 'fa-solid fa-sliders' }
  ];

  get initials(): string {
    const parts = this.adminName.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return 'AD';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  }

  toggleCollapsed(): void {
    this.collapsed = !this.collapsed;
    this.collapsedChange.emit(this.collapsed);
  }
}
