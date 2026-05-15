import { CommonModule } from '@angular/common';
import { Component, Input, booleanAttribute, inject } from '@angular/core';

import { AppTheme, ThemeService } from '../theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './theme-toggle.component.html',
  styleUrl: './theme-toggle.component.scss'
})
export class ThemeToggleComponent {
  private readonly themeService = inject(ThemeService);

  @Input({ transform: booleanAttribute })
  compact = false;

  readonly theme = this.themeService.theme;

  setTheme(theme: AppTheme): void {
    this.themeService.setTheme(theme);
  }
}