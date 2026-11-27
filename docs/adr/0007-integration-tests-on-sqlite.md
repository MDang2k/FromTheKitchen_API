# 7. Integration tests with WebApplicationFactory on SQLite

- **Status:** Accepted
- **Date:** 2026-11-19

## Context

Most of this API's risk is in how things fit together: routing, model binding, Identity, JWT validation, role checks, and the ownership and status rules. Unit tests with mocked `DbContext` and `UserManager` would mostly test the mocks. The tests should also run anywhere with `dotnet test`: in CI, and on a clone with no SQL Server and no secrets.

## Decision

- **The real app, in memory.** Tests start the real API with `WebApplicationFactory<Program>`, so the same `Program.cs`, middleware, Identity and JWT setup run.
- **Only infrastructure is swapped:**
  - SQL Server becomes a private SQLite in-memory database per test class, created with `EnsureCreated`, seed included.
  - The web root and the upload folder become temp folders.
  - The JWT key, seed Admin and CORS origin come from test settings.
- **Tests behave like a client.** They talk HTTP, register and log in real users, and send real tokens.
- **The OpenAPI document is checked too.** A test compares the generated document with the committed `docs/api/openapi.json`, so the published API spec can't drift from the code.

## Consequences

- **Fast and setup-free:** the whole suite runs in seconds, and every test exercises the real request pipeline.
- **Behaviour-focused:** refactors inside a controller don't break tests unless the behaviour changes.
- **SQLite is not SQL Server.** Migrations, SQL Server types and the retry strategy aren't exercised. `Program.cs` branches on the provider: `Migrate()` for SQL Server, `EnsureCreated()` otherwise.
- **Upgrade path:** run the same tests against a real SQL Server in Docker with Testcontainers.
- **Live checks are separate:** `scripts/live-smoke.ps1` covers the deployed app end to end.
