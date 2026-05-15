# Technical Specifications

## 1. System Summary

Auction System is a full-stack auction marketplace composed of an Angular 21 client and an ASP.NET Core Web API targeting .NET 10. The backend follows a Clean Architecture layout with CQRS, MediatR, FluentValidation, EF Core, and SignalR.

The solution is organized into these top-level areas:

- `client-app`: Angular frontend
- `src/AuctionSystem.API`: HTTP API, middleware, SignalR hub, startup
- `src/AuctionSystem.Application`: use cases, DTOs, validators, abstractions
- `src/AuctionSystem.Domain`: aggregates, entities, value objects, repository contracts
- `src/AuctionSystem.Infrastructure`: EF Core persistence, repositories, JWT and password services
- `tests`: xUnit unit and integration tests

## 2. Architecture Overview

### Frontend

- Angular 21 standalone-component application
- Route-based feature areas for auth, auctions, profile, settings, payment, and admin
- SignalR client service for live bid updates
- Theme initialization and dark/light mode support
- Browser localStorage used for theme preference and watchlist persistence

### API Layer

- ASP.NET Core controllers grouped by capability
- JWT bearer authentication
- Role-based authorization for admin endpoints
- Global exception handling via `ApiExceptionMiddleware`
- SignalR hub at `/hubs/auctions`
- Swagger enabled in Development

### Application Layer

- MediatR request/response handlers implement use cases
- FluentValidation validators execute through a MediatR pipeline behavior
- DTOs are returned from handlers instead of exposing EF entities directly
- Application abstractions isolate realtime notifications, repositories, security, and stores

### Domain Layer

- `Auction` aggregate encapsulates lifecycle, bid placement, and image ownership
- `Bid` entity models bid history per auction
- `User` entity models identity, role, activation, and profile state
- `Money` value object enforces amount and currency integrity

### Infrastructure Layer

- SQL Server persistence via EF Core 10
- Repository implementations for auctions, users, reports, settings, transactions, and payment methods
- JWT token generation and password hashing/verification services
- Automatic migration execution on API startup

## 3. Functional Scope

### Public and Authenticated User Flows

- Register and login with JWT issuance
- Browse active auctions with category and price filtering
- Fetch auction images through streamed API endpoints
- Create auctions with optional multipart image upload
- Place bids on active auctions with realtime broadcast to subscribed clients
- Report auctions for moderation review
- View personal bid history through `/api/auctions/my-bids`
- View and update user profile information
- Change password
- Backend payment-method endpoints are implemented, but the current Angular payment screen is still demo-only
- The current Won Auctions screen is demo-only and is not backed by an API yet

### Admin Flows

- View admin dashboard metrics and generated reports
- Manage users
- Manage auctions, including start, end, update, and delete operations
- Review flagged cases and resolve moderation reports
- Review transactions and process refunds
- Upsert admin-controlled system settings

## 4. API Surface Summary

### Authentication

- `POST /api/auth/login`
- `POST /api/auth/register`

### Auctions

- `GET /api/auctions`
- `POST /api/auctions` for JSON-based creation
- `POST /api/auctions` for multipart creation with images
- `GET /api/auctions/{auctionId}/images/{imageId}`
- `POST /api/auctions/{auctionId}/bids`
- `POST /api/auctions/{auctionId}/reports`
- `GET /api/auctions/my-bids`

### User Account

- `GET /api/users/profile`
- `PUT /api/users/profile`
- `POST /api/users/security/change-password`

### Payment Methods

- `POST /api/payment`
- `GET /api/payment`
- `DELETE /api/payment/{paymentMethodId}`

The payment API now binds all operations to the authenticated principal rather than trusting caller-supplied user IDs.

### Admin

- `GET /api/admin/users`
- `PUT /api/admin/users/{userId}`
- `DELETE /api/admin/users/{userId}`
- `GET /api/admin/auctions`
- `GET /api/admin/auctions/{auctionId}`
- `POST /api/admin/auctions/{auctionId}/start`
- `POST /api/admin/auctions/{auctionId}/end`
- `PUT /api/admin/auctions/{auctionId}`
- `DELETE /api/admin/auctions/{auctionId}`
- `GET /api/admin/moderation/cases`
- `POST /api/admin/moderation/cases/{caseId}/resolve`
- `GET /api/admin/transactions`
- `GET /api/admin/transactions/{transactionId}`
- `POST /api/admin/transactions/{transactionId}/refund`
- `GET /api/admin/reports/dashboard`
- `POST /api/admin/reports/generate`
- `PUT /api/admin/settings/{key}`

### Realtime

- SignalR hub: `/hubs/auctions`
- Client hub methods: `JoinAuction`, `LeaveAuction`
- Server event: `BidPlaced`

## 5. Security Model

- JWT bearer authentication is configured from the `Jwt` settings section.
- Admin routes use an explicit `AdminOnly` authorization policy.
- User-specific endpoints typically derive the user ID from the authenticated claims principal.
- Password changes require the current password.
- Password hashes are stored with salt and iteration metadata.
- API controllers use a shared exception middleware to normalize error responses.

## 6. Persistence and Data Behavior

- SQL Server is the primary relational store.
- EF Core migrations live under `src/AuctionSystem.Infrastructure/Persistence/Migrations`.
- The API attempts to apply migrations automatically at startup.
- Development seeding can create:
  - 100 users
  - 100 auctions
  - sample payment methods
  - admin system settings
- Development seed passwords use a fixed local-only credential.

Important persistence entities include:

- `User`
- `Auction`
- `Bid`
- `AuctionImage`
- `PaymentMethod`
- `AuctionReport`
- `AdminTransactionRefund`
- `AdminSystemSetting`

## 7. Runtime Configuration

Primary configuration keys:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SigningKey`
- `Jwt:AccessTokenMinutes`
- `DatabaseSeeding:Enabled`
- `DatabaseSeeding:ResetDatabaseOnStartup`

Development launch profiles expose:

- HTTP: `http://localhost:5266`
- HTTPS: `https://localhost:7196`

The Angular development proxy forwards `/api` and `/hubs` to the HTTP API endpoint.

## 8. Testing Strategy

- Unit tests target domain rules and application handlers.
- Integration tests target end-to-end API behavior using `WebApplicationFactory`, including controller endpoints, startup wiring, realtime hub flows, concurrent bidding scenarios, and JWT authentication edge cases.
- Integration tests commonly use EF Core InMemory to isolate database behavior.
- Frontend browser tests use Playwright against the Angular application, with a default mocked profile for broad UI branch coverage across guest, authenticated-user, seller, and admin flows.
- The mocked Playwright suite covers auth redirects, expired sessions, login/register/logout, marketplace search and filtering, pagination, bid/report/watchlist flows, auction creation validation, profile edit/error flows, local payment-method UI behavior, and admin dashboard, users, auctions, transactions, reports, settings, and moderation branches without requiring a running backend.
- A separate live-backend Playwright profile exists for smoke-level true end-to-end runs against a running frontend plus API environment.
- The frontend includes Angular build tooling and a Vitest-based test script.

## 9. Known Limitations and Current Gaps

- The Won Auctions frontend route is currently demo-only, renders placeholder client-side data, and has no corresponding API endpoint.
- The watchlist feature is currently browser-persisted per authenticated user and is not synchronized through the backend.
- The payment-method frontend screen is currently demo-only and uses local component state instead of the implemented payment API.
- The default Playwright browser suite relies on mocked API and realtime responses; the separate live Playwright profile is currently smoke-focused rather than full regression coverage.
- The optional live admin Playwright smoke requires external credentials provided through environment variables.
- Cross-browser and visual-regression Playwright coverage are not currently enabled.
- The mocked suite intentionally cannot validate backend serialization, database state, auth policy wiring, or SignalR transport behavior; those are covered by backend integration tests and the smaller live smoke profile.
- Placeholder JWT configuration values still exist in checked-in appsettings files and must be overridden for any shared or production environment.
- Environment-specific deployment pipeline files should be removed or sanitized before a public release.

## 10. Public Repository Readiness Notes

Before publishing the repository publicly:

- Remove or sanitize deployment YAML files and internal deployment paths.
- Move real secrets to environment variables, secret stores, or CI/CD secret variables.
- Keep public documentation aligned with current implementation, especially the demo-only frontend areas for payment management and won-auctions.
