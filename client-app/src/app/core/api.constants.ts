function getBaseHref(): string {
  const href = globalThis.document?.querySelector('base')?.getAttribute('href')?.trim() || '/';
  const origin = globalThis.location?.origin || 'http://localhost';

  let pathname: string;

  try {
    pathname = new URL(href, origin).pathname;
  } catch {
    pathname = href;
  }

  if (!pathname.startsWith('/')) {
    pathname = `/${pathname}`;
  }

  return pathname.replace(/\/+$/, '');
}

export const API_BASE_URL = `${getBaseHref()}/api`;
