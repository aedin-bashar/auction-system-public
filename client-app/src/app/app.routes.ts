import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './auth/auth.guards';

import { LoginComponent } from './auth/login/login.component';
import { RegisterComponent } from './auth/register/register.component';
import { ForgotPasswordComponent } from './auth/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './auth/reset-password/reset-password.component';
import { CreateAuctionComponent } from './auctions/create-auction/create-auction.component';
import { HomeComponent } from './auctions/home/home.component';
import { MyBidsComponent } from './auctions/my-bids/my-bids.component';
import { WatchlistComponent } from './auctions/watchlist/watchlist.component';
import { WonAuctionsComponent } from './auctions/won-auctions/won-auctions.component';
import { AppShellComponent } from './layout/app-shell/app-shell.component';
import { PaymentMethodsComponent } from './payment/payment-methods/payment-methods.component';
import { ProfileComponent } from './profile/profile/profile.component';
import { SettingsComponent } from './settings/settings/settings.component';
import { AdminDashboardComponent } from './admin/admin-dashboard/admin-dashboard.component';
import { AdminUsersComponent } from './admin/admin-users/admin-users.component';
import { AdminAuctionsComponent } from './admin/admin-auctions/admin-auctions.component';
import { AdminAuctionDetailComponent } from './admin/admin-auction-detail/admin-auction-detail.component';
import { AdminTransactionsComponent } from './admin/admin-transactions/admin-transactions.component';
import { AdminReportsComponent } from './admin/admin-reports/admin-reports.component';
import { AdminSystemSettingsComponent } from './admin/admin-system-settings/admin-system-settings.component';
import { AdminFlaggedCasesComponent } from './admin/admin-flagged-cases/admin-flagged-cases.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },

  {
    path: '',
    component: AppShellComponent,
    children: [
      { path: '', component: HomeComponent, pathMatch: 'full' },
      { path: 'create', component: CreateAuctionComponent },
      { path: 'my-bids', component: MyBidsComponent, canActivate: [authGuard] },
      { path: 'won-auctions', component: WonAuctionsComponent, canActivate: [authGuard] },
      { path: 'watchlist', component: WatchlistComponent, canActivate: [authGuard] },

      { path: 'profile', component: ProfileComponent, canActivate: [authGuard] },
      { path: 'profile/edit', redirectTo: 'profile', pathMatch: 'full' },
      { path: 'payment-methods', component: PaymentMethodsComponent, canActivate: [authGuard] },
      { path: 'settings', component: SettingsComponent, canActivate: [authGuard] },

      { path: 'admin/dashboard', component: AdminDashboardComponent, canActivate: [adminGuard] },
      { path: 'admin/users', component: AdminUsersComponent, canActivate: [adminGuard] },
      { path: 'admin/auctions', component: AdminAuctionsComponent, canActivate: [adminGuard] },
      { path: 'admin/auctions/:auctionId', component: AdminAuctionDetailComponent, canActivate: [adminGuard] },
      { path: 'admin/flagged-cases', component: AdminFlaggedCasesComponent, canActivate: [adminGuard] },
      { path: 'admin/transactions', component: AdminTransactionsComponent, canActivate: [adminGuard] },
      { path: 'admin/reports', component: AdminReportsComponent, canActivate: [adminGuard] },
      { path: 'admin/settings', component: AdminSystemSettingsComponent, canActivate: [adminGuard] }
    ]
  },

  { path: '**', redirectTo: '' }
];
