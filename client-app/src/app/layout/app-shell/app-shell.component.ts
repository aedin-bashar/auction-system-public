import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';

import { AuthService } from '../../auth/auth.service';
import { AuthSession } from '../../auth/auth.models';
import { LoadingService } from '../../core/loading.service';
import { AdminSidebarComponent } from '../admin-sidebar/admin-sidebar.component';
import { NavbarComponent } from '../navbar/navbar.component';
import { UserSidebarComponent } from '../user-sidebar/user-sidebar.component';

type SidebarMode = 'none' | 'user' | 'admin';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    NavbarComponent,
    UserSidebarComponent,
    AdminSidebarComponent
  ],
  templateUrl: 'app-shell.component.html',
  styleUrls: ['app-shell.component.scss']
})
export class AppShellComponent {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly loadingService = inject(LoadingService);
  private readonly userSidebarCollapsedStorageKey = 'auction-system:user-sidebar-collapsed';
  private readonly adminSidebarCollapsedStorageKey = 'auction-system:admin-sidebar-collapsed';

  session: AuthSession | null = this.authService.getSession();
  readonly currentYear = new Date().getFullYear();
  readonly isLoading$ = this.loadingService.isLoading$;
  sidebarMode: SidebarMode = this.getSidebarMode(this.router.url);
  showSearch = this.isAuctionsListPage(this.router.url);
  userSidebarCollapsed = this.getStoredCollapsedState(this.userSidebarCollapsedStorageKey);
  adminSidebarCollapsed = this.getStoredCollapsedState(this.adminSidebarCollapsedStorageKey);

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.sidebarMode = this.getSidebarMode(event.urlAfterRedirects);
        this.showSearch = this.isAuctionsListPage(event.urlAfterRedirects);
      });
  }

  onLogout(): void {
    this.authService.logout();
    this.session = null;
    void this.router.navigate(['/']);
  }

  onSearchChanged(term: string): void {
    void this.router.navigate(['/'], {
      queryParams: { search: term || null },
      queryParamsHandling: 'merge'
    });
  }

  onUserSidebarCollapsedChange(collapsed: boolean): void {
    this.userSidebarCollapsed = collapsed;
    this.setStoredCollapsedState(this.userSidebarCollapsedStorageKey, collapsed);
  }

  onAdminSidebarCollapsedChange(collapsed: boolean): void {
    this.adminSidebarCollapsed = collapsed;
    this.setStoredCollapsedState(this.adminSidebarCollapsedStorageKey, collapsed);
  }

  private getStoredCollapsedState(storageKey: string): boolean {
    try {
      return globalThis.localStorage?.getItem(storageKey) === 'true';
    } catch {
      return false;
    }
  }

  private setStoredCollapsedState(storageKey: string, isCollapsed: boolean): void {
    try {
      globalThis.localStorage?.setItem(storageKey, String(isCollapsed));
    } catch {
    }
  }

  private getSidebarMode(url: string): SidebarMode {
    if (url.startsWith('/admin')) {
      return 'admin';
    }

    if (
      url.startsWith('/profile') ||
      url.startsWith('/payment-methods') ||
      url.startsWith('/settings') ||
      url.startsWith('/my-bids') ||
      url.startsWith('/watchlist') ||
      url.startsWith('/create')
    ) {
      return 'user';
    }

    return 'none';
  }

  private isAuctionsListPage(url: string): boolean {
    const path = url.split('?')[0];
    return path === '/';
  }
}
