import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

applyStoredThemePreference();

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));

function applyStoredThemePreference(): void {
  if (typeof document === 'undefined') {
    return;
  }

  const storageKey = 'auction-system:theme';
  let theme = 'light';

  try {
    const storedTheme = globalThis.localStorage?.getItem(storageKey);
    if (storedTheme === 'dark' || storedTheme === 'light') {
      theme = storedTheme;
    } else if (globalThis.matchMedia?.('(prefers-color-scheme: dark)').matches === true) {
      theme = 'dark';
    }
  } catch {
    if (globalThis.matchMedia?.('(prefers-color-scheme: dark)').matches === true) {
      theme = 'dark';
    }
  }

  document.documentElement.setAttribute('data-theme', theme);
  document.documentElement.style.colorScheme = theme;
}
