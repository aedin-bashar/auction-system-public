# Auction System Client

This Angular 21 application is the frontend for the Auction System project.

## Current State

- The payment-method screen is currently demo-only and uses local component state instead of the backend payment API.
- The Won Auctions screen is currently demo-only and renders placeholder data.
- Both of these areas should be treated as future frontend integrations rather than finished end-to-end features.

## Development server

To start a local development server, run:

```bash
npm start
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
npx ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
npx ng generate --help
```

## Building

To build the project run:

```bash
npm run build
```

This compiles the app and writes the build artifacts to `dist/client-app/`.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
npm test
```

## End-to-end tests

This project includes a Playwright suite that covers guest, authenticated user, seller, and admin browser flows.

Install the browser runtime once:

```bash
npm run test:e2e:install
```

Run the suite:

```bash
npm run test:e2e
```

The default suite is mocked: it starts the Angular dev server automatically and uses mocked API plus realtime responses, so the backend API does not need to be running. It covers auth redirects, expired sessions, login/register/logout, marketplace search and filtering, pagination, bid/report/watchlist flows, auction creation validation, profile edit/error flows, payment-method UI behavior, and admin dashboard, users, auctions, transactions, reports, settings, and moderation paths.

Run the live-backend smoke profile against a real backend:

```bash
npm run test:e2e:live
```

Live profile notes:

- Start the backend API first so the Angular dev proxy can forward `/api` and `/hubs` requests.
- The live profile uses `playwright.live.config.ts`.
- Set `PLAYWRIGHT_LIVE_ADMIN_EMAIL` and `PLAYWRIGHT_LIVE_ADMIN_PASSWORD` to enable the optional admin smoke test.
- The live profile is intentionally smoke-level; the mocked suite is the primary fast browser regression suite.

Useful alternatives:

```bash
npm run test:e2e:headed
npm run test:e2e:ui
npm run test:e2e:live:headed
npx playwright show-report
```

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.

## Future Improvements

- Add optional TOTP-based two-factor authentication back to the account experience in a later iteration.
- Replace the demo-only payment-method screen with API-backed flows.
- Replace the demo-only Won Auctions screen with real backend data and history.
- Expand the live-backend Playwright profile beyond smoke coverage and add cross-browser plus visual regression checks.
