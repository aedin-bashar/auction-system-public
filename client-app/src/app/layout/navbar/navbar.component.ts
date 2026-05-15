import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { AuthSession } from '../../auth/auth.models';
import { ThemeToggleComponent } from '../../core/theme-toggle/theme-toggle.component';
import { UserDropdownComponent } from '../user-dropdown/user-dropdown.component';

type NavItem = {
  label: string;
  route: string;
};

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, UserDropdownComponent, ThemeToggleComponent],
  templateUrl: 'navbar.component.html',
  styleUrls: ['navbar.component.scss']
})
export class NavbarComponent {
  @Input() session: AuthSession | null = null;
  @Input() showSearch = false;

  @Output() readonly searchChanged = new EventEmitter<string>();
  @Output() readonly logoutRequested = new EventEmitter<void>();

  query = '';

  readonly navItems: NavItem[] = [
    { label: 'Auctions', route: '/' },
    { label: 'My Bids', route: '/my-bids' },
    { label: 'Watchlist', route: '/watchlist' }
  ];

  onSearch(value: string): void {
    this.query = value;
    this.searchChanged.emit(value.trim());
  }

  onSearchSubmit(): void {
    this.searchChanged.emit(this.query.trim());
  }

  onLogout(): void {
    this.logoutRequested.emit();
  }
}
