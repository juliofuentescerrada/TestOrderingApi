# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Orders API in .NET 8: Clean Architecture + MediatR + EF Core (SQLite) + JWT auth. Code comments and docs are written in Spanish; keep that convention.

## Context: technical interview exercise

`ENUNCIADO-candidato.pdf` (kept local and untracked on purpose; never commit it) is the brief for a 55-min pair-programming interview where AI use is expected and evaluated (how it is steered, and how its output is reviewed). Ask about ambiguities instead of assuming; that is part of the test. Never put credentials or personal/client data in prompts.

**Part 1 – `POST /api/orders`** (done; body: `customerId` + `lines[]` of `{sku, quantity, unitPrice}`):
1. MediatR command + handler, persisted via `IOrderRepository`.
2. Input validation → `400` with a clear `ProblemDetails`. Rules: 1–50 lines, non-empty `sku`, `quantity > 0`, `unitPrice >= 0`, `customerId` present.
3. Policy `orders:write`: `401` without a token, `403` without the scope.
4. `201 Created`, with `Location` → `GET /api/orders/{id}` (route name `GetOrderById`) and the `OrderDto` as the body.
5. At least one meaningful test.
- Optional, not implemented here: an `Idempotency-Key` header (string, ≤64 chars). Same key + same body → same order. Same key + different body → choose and justify the response.

**Part 2** is reviewing a PR that adds `GET /api/customers/{customerId}/orders` (id, date, total with a 10% loyalty discount over 1,000, line count, plus an audit event per query). Review it for security/authz, query performance and money logic.

Design decisions are recorded as ADRs in `docs/adr/` (Spanish, one per decision, continue the numbering). Non-obvious fixes in code carry a one-line Spanish comment explaining why.

## Commands

```bash
dotnet restore
dotnet build
dotnet test                                                   # all tests
dotnet test --filter "FullyQualifiedName~OrdersApiTests"      # one class
dotnet test --filter "FullyQualifiedName~Get_unknown_order_returns_404"  # one test
dotnet run --project src/Ordering.Api                         # http://localhost:5080, Swagger at /swagger (Development)
```

`Directory.Build.props` sets `RollForward=Major`, so the app runs on a newer runtime when .NET 8 isn't installed. The integration tests that return a JSON body still need the **.NET 8 ASP.NET Core runtime**. On 9/10 they fail with `PipeWriter 'ResponseBodyPipeWriter' does not implement PipeWriter.UnflushedBytes`, because `Microsoft.AspNetCore.Mvc.Testing` 8.x's TestHost doesn't support the newer MVC JSON formatter. There is no separate linter.

## Architecture

Dependency direction: `Api → Application → Domain`, `Infrastructure → Application/Domain`.

- **Domain** (`Order`, `OrderLine`): entities with private setters, created via factory methods (`Order.Create`, `AddLine`). Invariants are enforced in the entity and violations throw `DomainException` (e.g. max 50 lines, quantity > 0, non-negative price).
- **Application**: one folder per use case under `Orders/<UseCase>/` holding the MediatR request + handler together in one file (see `GetOrderById/GetOrderByIdQuery.cs`). Handlers are auto-registered by `AddApplication()`.
  - CQRS-ish split of persistence contracts: **reads** go through `IOrderReadService` (no-tracking, returns `OrderDto`); **writes** go through `IOrderRepository` (tracked entities + `SaveChangesAsync`).
  - `OrderDto.From(order)` is the entity→DTO mapping; `Status` is serialized as a string.
  - `TimeProvider` is registered (singleton) for testable timestamps — inject it rather than calling `DateTime.UtcNow`.
- **Infrastructure**: EF Core implementations + `AppDbContext`/configurations. `AddInfrastructure()` registers repositories, but the `DbContext` itself is registered in `Program.cs` with a lazily-read connection string so tests can override it.
- **Api**: thin controllers that only `_sender.Send(...)` and map results to HTTP. No migrations — the schema is created by `EnsureCreated()` at startup, so model changes require deleting the local DB. With `dotnet run --project src/Ordering.Api` it lives at `src/Ordering.Api/ordering.db` (plus `-shm`/`-wal`), not at the repo root.
  - `POST /api/orders` validates in two layers: DataAnnotations on `Contracts/CreateOrderRequest` (auto `400 ValidationProblemDetails`) and `DomainExceptionHandler`, which maps any `DomainException` to `400 ProblemDetails` app-wide.

### Auth

- Permissions come from the space-separated JWT `scope` claim (OAuth 2.0 style); `MapInboundClaims = false`.
- Policies are named after the scope (`Policies.OrdersRead = "orders:read"`, `Policies.OrdersWrite = "orders:write"`) and defined in `Program.cs` via `Policies.HasScope`. The controller has `[Authorize]` at class level plus `[Authorize(Policy = ...)]` per action.
- Signing key: `Jwt:SigningKey` (only in `appsettings.Development.json`; startup throws if missing). Issuer/audience in `appsettings.json`.

## Tests (`tests/Ordering.Tests`, xUnit)

- Domain unit tests in `OrderTests.cs`; integration tests use `IClassFixture<ApiFactory>`.
- `ApiFactory` boots the real app in the `Testing` environment with its own temp SQLite file and test JWT settings.
- Get tokens with `TestTokens.Create("orders:read", ...)`; seed data directly via `AppDbContext` from `_factory.Services.CreateScope()` (see `OrdersApiTests.SeedOrder`).
- `OrdersApiTests` is the reference pattern for endpoint tests: cover 401 (no token), 403 (missing scope), and the happy/404 paths.

## OpenSpec

The repo uses OpenSpec (`openspec/`, `schema: spec-driven`) for change proposals; the `openspec-*` skills drive the workflow (propose → apply → verify → archive). Current capability: `openspec/specs/order-creation`; archived changes live in `openspec/changes/archive/`.
