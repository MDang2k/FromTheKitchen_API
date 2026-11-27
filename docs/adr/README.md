# Architecture decision records

Short notes on the decisions that shape this API: what was decided, why, and what it costs. They use [Michael Nygard's format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions): context, decision, consequences.

A decision that changes later gets a new ADR that supersedes the old one. Old ADRs aren't rewritten.

| # | Decision | Status |
| --- | --- | --- |
| [0001](0001-jwt-bearer-authentication.md) | JWT bearer tokens for authentication | Accepted |
| [0002](0002-sql-server-ef-core-migrations-at-startup.md) | SQL Server with EF Core code-first migrations, applied at startup | Accepted |
| [0003](0003-seed-first-admin-from-configuration.md) | Seed the first Admin from configuration | Accepted |
| [0004](0004-server-is-source-of-truth-for-orders.md) | The server is the source of truth for orders | Accepted |
| [0005](0005-uploaded-images-in-a-configurable-folder.md) | Store uploaded images in a configurable folder | Accepted |
| [0006](0006-azure-app-service-and-github-actions.md) | Host on Azure App Service and Azure SQL, deploy with GitHub Actions | Accepted |
| [0007](0007-integration-tests-on-sqlite.md) | Integration tests with WebApplicationFactory on SQLite | Accepted |
| [0008](0008-uniform-api-response-envelope.md) | One response envelope for every endpoint | Accepted |

To add one, copy the newest ADR, give it the next number, and add it to this table.
