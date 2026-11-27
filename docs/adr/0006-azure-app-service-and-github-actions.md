# 6. Host on Azure App Service and Azure SQL, deploy with GitHub Actions

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

The project is a .NET portfolio piece that should run somewhere public at little or no cost. It already uses SQL Server. The code lives on GitHub.

## Decision

- **API:** Azure App Service (Linux, .NET 10, Free F1 plan), running the published app directly without a container.
- **Database:** Azure SQL Database, free serverless offer, auto-pausing when idle.
- **Configuration:** App Service settings, with `__` in place of `:`. Covers the connection string, `ApiSettings__Secret`, `SeedAdmin__*`, `Cors__AllowedOrigins__0` and `ImageStorage__Path`.
- **Health:** `GET /health` returns `Healthy` and is used as App Service's health check path.
- **CI/CD:** `.github/workflows/ci-cd.yml` runs the tests on every push and pull request. On pushes to `main` it publishes and deploys with `azure/webapps-deploy`, authenticated by a publish profile stored as a repository secret. The deploy job is skipped until the `AZURE_WEBAPP_NAME` variable exists.

## Consequences

- **Stays close to local development.** No Dockerfile, the same SQL engine, and no provider switch.
- **Free tier limits bite:**
  - F1 allows about 60 CPU minutes a day and 15 restarts an hour. A crash loop uses those up and gets the app stopped (`QuotaExceeded`).
  - The app and the database both sleep when idle, so the first request afterwards is slow.
- **The publish profile needs SCM basic auth turned on** and is a long-lived credential. OIDC (`azure/login` with a federated credential) would remove it.
- **App Service ends TLS in front of the app.** "HTTPS Only" is turned on in App Service rather than enforced by the app.
- **Alternative considered:** a container on Render or Fly.io with PostgreSQL. Cheaper at scale, but it meant switching the EF Core provider and regenerating every migration.
