# 2. SQL Server with EF Core code-first migrations, applied at startup

- **Status:** Accepted
- **Date:** 2026-11-19 (SQL Server and EF Core); 2026-11-25 (migrating at startup)

## Context

The data is relational: users, orders, order lines and menu items, with foreign keys between them. ASP.NET Core Identity already ships an EF Core store. The app runs on SQL Server Express locally and Azure SQL Database in production, which is the same engine, so migrations behave the same in both.

A brand-new cloud database needs its schema without anyone running `dotnet ef database update` by hand.

## Decision

- **EF Core 10, code-first.** The C# model classes are the schema; migrations live in `FromTheKitchen_API/Migrations`.
- **Migrate at startup.** `Program.cs` calls `Database.Migrate()` before the app accepts requests. The menu is seeded with `HasData` in the same migrations.
- **Retry transient errors.** SQL Server uses `EnableRetryOnFailure()`, so a serverless Azure SQL database waking from auto-pause doesn't crash startup.
- **Tests use SQLite.** It can't apply SQL Server migrations, so test runs call `EnsureCreated()` instead ([ADR 0007](0007-integration-tests-on-sqlite.md)).

## Consequences

- **Deploys are one step:** push, and the schema updates itself on the next start.
- **Fine for one instance only.** Several instances starting at once would race to apply the same migration. Before scaling out, move migrations into the pipeline as an EF Core migration bundle (`dotnet ef migrations bundle`).
- **A bad migration takes the app down**, because it runs before the app serves anything. There's no separate, reviewable migration step.
- **Untested on SQL Server.** SQL-Server-specific behaviour, including the migrations themselves, isn't covered by the test suite.
