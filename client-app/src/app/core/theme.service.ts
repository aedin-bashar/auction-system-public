import { DOCUMENT } from '@angular/common';
import { Injectable, WritableSignal, inject, signal } from '@angular/core';

export type AppTheme = 'dark' | 'light';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly storageKey = 'auction-system:theme';
  private readonly themeState: WritableSignal<AppTheme> = signal(this.resolveInitialTheme());

  readonly theme = this.themeState.asReadonly();

  constructor() {
    this.applyTheme(this.themeState());
  }

  setTheme(theme: AppTheme): void {
    if (this.themeState() === theme) {
      return;
    }

    this.themeState.set(theme);
    this.applyTheme(theme);

    try {
      globalThis.localStorage?.setItem(this.storageKey, theme);
    } catch {
    }
  }

  toggleTheme(): void {
    this.setTheme(this.themeState() === 'dark' ? 'light' : 'dark');
  }

  private resolveInitialTheme(): AppTheme {
    try {
      const storedTheme = globalThis.localStorage?.getItem(this.storageKey);
      if (storedTheme === 'dark' || storedTheme === 'light') {
        return storedTheme;
      }
    } catch {
    }

    return globalThis.matchMedia?.('(prefers-color-scheme: dark)').matches === true ? 'dark' : 'light';
  }

  private applyTheme(theme: AppTheme): void {
    const root = this.document?.documentElement;
    if (!root) {
      return;
    }

    root.setAttribute('data-theme', theme);
    root.style.colorScheme = theme;
  }
}