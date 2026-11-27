# From The Kitchen

[![CI/CD](https://github.com/MDang2k/FromTheKitchen_API/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/MDang2k/FromTheKitchen_API/actions/workflows/ci-cd.yml)
[![Web](https://github.com/MDang2k/FromTheKitchen_API/actions/workflows/web.yml/badge.svg)](https://github.com/MDang2k/FromTheKitchen_API/actions/workflows/web.yml)

**From The Kitchen** is a small restaurant ordering app. Customers browse a menu, place pickup orders and track them. Admins manage the menu and move orders through to pickup.

**Live demo:** https://ftk-api-dang-a5hdhxc4a4c8asa5.italynorth-01.azurewebsites.net/. It runs on Azure's free tier, so the first visit after a quiet spell can take up to a minute while the app and database wake up.

This is a personal project. I built it to practise backend development with C# and ASP.NET Core: REST API design, Entity Framework Core, ASP.NET Core Identity, JWT authentication, role-based authorization, integration testing and cloud deployment.

This repository holds both halves:
- **`FromTheKitchen_API/`**: the ASP.NET Core API described below. In production it also serves the frontend.
- **`FromTheKitchen_Web/`**: the React frontend that calls it (see [Frontend](#frontend)).

**Design docs:** the [data model (ER diagram)](docs/er-diagram.md), the [architecture decision records](docs/adr/README.md), and the [OpenAPI spec](docs/api/openapi.json), which is generated from the code and checked by a test. They're all indexed in [docs/](docs/README.md).

## Tech stack

| Area | Technology |
| --- | --- |
| Framework | ASP.NET Core Web API on **.NET 10** |
| Data access | Entity Framework Core 10 (code-first, migrations) |
| Database | SQL Server (SQL Express locally) |
| Users and roles | ASP.NET Core Identity (`IdentityDbContext<ApplicationUser>`) |
| Authentication | JWT Bearer tokens (`System.IdentityModel.Tokens.Jwt`) |
| API docs | Built-in OpenAPI document + [Scalar](https://github.com/scalar/scalar) UI |
| File storage | Menu images saved to a configurable folder (`wwwroot/images` locally) and served as static files |
| Frontend | React 19, TypeScript, Vite, Tailwind CSS 4, React Router 7 |
| Tests | xUnit integration tests with `WebApplicationFactory` on SQLite in-memory |
| Hosting | Azure App Service (Linux) and Azure SQL Database, deployed by GitHub Actions |

## What it does

### Authentication and roles
- `POST /api/Auth/register` creates a user with ASP.NET Core Identity. New users are `Customer` by default. Asking for `Admin` only works when the request carries an Admin's JWT; otherwise it returns 403.
- At startup the API applies pending migrations, creates the `Admin` and `Customer` roles, and creates the first Admin from the `SeedAdmin:Email` / `SeedAdmin:Password` settings if that account doesn't exist yet. An existing account with that email is only promoted if the password matches, so a stranger who registered the address first can't become Admin. A seed password that breaks Identity's rules stops startup with the reason.
- `POST /api/Auth/login` checks the password and returns a JWT, valid for 7 days. The token holds the user's id, name, email and role, with Admin winning if a user has several roles. The email and role are also in the response body.
- The API validates the token's signature and lifetime on every request that needs authorization. `[Authorize(Roles = "Admin")]` limits endpoints to admins.

### Menu
- The database is seeded with 10 dishes (appetizers, salads, entrees, desserts, beverages), each with a price, a category, an optional tag such as "Best Seller" or "Chef's Special", and an image.
- Menu items are created and updated with `multipart/form-data`, so the image file is uploaded in the same request. It's saved to the folder in `ImageStorage:Path` (default `wwwroot/images`) and served at `/images/<file name>`.
- Uploads are saved under a new random name, so two files called `pizza.jpg` never overwrite each other and a name like `../../x.jpg` can't write outside the folder. Only JPG, PNG, WebP and GIF files up to 5 MB are accepted, because images are served from the API's own origin.
- Updating an item without a file keeps its image. Sending a new file replaces it and deletes the old one.
- An item that appears in past orders can't be deleted (409), because the order lines' foreign key cascades and would erase order history. Edit it instead.

### Orders
- An order is an `OrderHeader` (pickup name, phone, email, user, total, item count, status, date) with a list of `OrderDetail` lines (menu item, quantity, name and price at time of order).
- To place an order the client sends only menu item ids and quantities. The server looks up names and prices and works out the total, so nobody can set their own price. Empty orders, unknown items and quantities below 1 are rejected. The header and its lines are saved in one transaction.
- Orders can be listed (newest first) or fetched one by one with their lines and menu items.
- Pickup details (name, phone, email) can be updated. Only fields that are sent are changed.
- Status follows a simple workflow. New orders start as `Confirmed`, then move to `Ready For Pick Up`, then `Completed`. An order can be `Cancelled` until it is completed. Any other change, such as skipping a step or reopening a cancelled order, returns 400 and saves nothing.
- Customers can rate each item of their own orders (`Rating` on `OrderDetail`).

### Who can do what with orders
- **Customers** only see and update their own orders. The order's owner comes from the JWT, not the request body, so nobody can place an order for someone else. Customers can edit pickup details and cancel their own orders.
- **Admins** see every order (optionally filtered with `?userId=`) and are the only ones who move an order to `Ready For Pick Up` or `Completed`.
- Touching someone else's order returns 403.

### Frontend

A single-page React app in `FromTheKitchen_Web/`:

| Page | Route | Who |
| --- | --- | --- |
| Home and menu, filtered by category, with add-to-cart | `/` | Anyone |
| Log in / register | `/login`, `/register` | Anyone |
| Cart (kept in `localStorage`) | `/cart` | Anyone |
| Checkout (pickup details) and confirmation | `/checkout`, `/order-confirmation` | Logged in |
| My orders: status and cancelling | `/my-orders` | Logged in |
| Menu management: create, edit, delete dishes with images | `/menu-management` | Admin |
| Order management: all orders, move them through the workflow | `/order-management` | Admin |

- **Auth in the browser:** the JWT is kept in `localStorage` and sent as a Bearer header. A route guard sends logged-out users to the login page, then back afterwards, and shows customers an "Admins only" message on admin pages. That only hides pages: the API checks the token and role on every request anyway.
- **API address:**
  - **In production** the API serves the built app, so it calls `/api/...` on its own origin and needs no CORS.
  - **In development** Vite runs on `http://localhost:5173` and calls `https://localhost:7145`. That origin is allowed in `Cors:AllowedOrigins` (`appsettings.Development.json`). `VITE_API_URL` overrides the API address.

### Consistent responses
Every endpoint except `/health` and the `AuthTest` ones wraps its result in the same `ApiResponse` shape, so the frontend handles every call the same way:

```json
{
  "statusCode": 200,
  "isSuccess": true,
  "errorMessages": [],
  "result": { }
}
```

## Endpoints

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| POST | `/api/Auth/register` | – (Admin to create an Admin) | Register a user |
| POST | `/api/Auth/login` | – | Log in and receive a JWT |
| GET | `/api/MenuItem` | – | List all menu items |
| GET | `/api/MenuItem/{id}` | – | Get one menu item |
| POST | `/api/MenuItem` | Admin | Create a menu item with an image (form-data) |
| PUT | `/api/MenuItem?id={id}` | Admin | Update a menu item, optionally replacing its image (form-data) |
| DELETE | `/api/MenuItem?id={id}` | Admin | Delete a menu item and its image (409 if it was ever ordered) |
| GET | `/api/OrderHeader?userId={id}` | Logged in | List orders: customers get their own, admins get all or one user's |
| GET | `/api/OrderHeader/{orderId}` | Owner or Admin | Get one order |
| POST | `/api/OrderHeader` | Logged in | Place an order for yourself |
| PUT | `/api/OrderHeader/{orderId}` | Owner or Admin | Update pickup details or cancel (owner); move to the next status (Admin) |
| PUT | `/api/OrderDetails/{orderDetailId}` | Owner | Rate one item of your order |
| GET | `/api/AuthTest` | Any logged-in user | Checks that a JWT works |
| GET | `/api/AuthTest/{value}` | Admin | Checks that the Admin role works |
| GET | `/health` | – | Returns `Healthy` when the app is up (used by the host's health check) |

In development the full interactive reference is at `/scalar/v1`, where you can paste a JWT and try every endpoint. The same contract is committed as [docs/api/openapi.json](docs/api/openapi.json) (see [docs/](docs/README.md#the-api-spec) for how it's kept up to date).

## Project structure

```
├── .github/workflows/
│   ├── ci-cd.yml               # Test, then deploy main (API + built frontend) to Azure
│   └── web.yml                 # Frontend: lint and build frontend changes
├── docs/
│   ├── er-diagram.md           # Data model (Mermaid ER diagram)
│   ├── adr/                    # Architecture decision records
│   └── api/openapi.json        # OpenAPI spec, generated and checked by a test
├── scripts/live-smoke.ps1      # End-to-end check against the deployed app
├── FromTheKitchen_Web/         # React frontend (see "Frontend" and "Running locally")
│   └── src/
│       ├── pages/              # Home, auth, cart/checkout, orders, admin pages
│       ├── components/         # Header, footer, route guard, shared UI
│       ├── store/cart.ts       # Cart kept in localStorage
│       └── utility/            # API client, token helpers, routes and types
├── FromTheKitchen_API.slnx
├── FromTheKitchen_API/
│   ├── Program.cs              # Services, CORS, health check, frontend fallback; migrates the DB and seeds the Admin at startup
│   ├── appsettings.json        # Connection string (the JWT secret lives in user secrets)
│   ├── Controllers/
│   │   ├── AuthController.cs       # Register / login, issues JWTs
│   │   ├── MenuItemController.cs   # Menu CRUD and image upload
│   │   ├── OrderHeaderController.cs  # Place, list, update orders
│   │   ├── OrderDetailsController.cs # Rate order items
│   │   └── AuthTestController.cs   # Endpoints for testing auth and roles
│   ├── Data/
│   │   ├── ApplicationDbContext.cs # DbSets and menu seed data
│   │   └── AdminSeeder.cs          # Roles and the first Admin from config
│   ├── Models/
│   │   ├── ApplicationUser.cs, MenuItem.cs, OrderHeader.cs, OrderDetail.cs
│   │   ├── ApiResponse.cs          # Shared response wrapper
│   │   └── Dto/                    # Request/response DTOs
│   ├── Migrations/             # EF Core migrations (Identity, menu, seed, orders, ratings)
│   ├── Utility/SD.cs           # Role names and order status constants
│   └── wwwroot/images/         # Seeded menu images (the deploy adds the built frontend to wwwroot)
└── FromTheKitchen_API.Tests/
    ├── Infrastructure/ApiFactory.cs  # Runs the real API in memory on SQLite
    ├── AuthTests.cs                  # Register, login, JWT, admin rules
    ├── AdminSeederTests.cs           # First Admin from config
    ├── HostingTests.cs               # /health, CORS, frontend fallback routes
    ├── MenuItemTests.cs              # Public menu, admin-only changes, image upload
    ├── OrderTests.cs                 # Ownership, status workflow, ratings
    └── OpenApiDocumentTests.cs       # docs/api/openapi.json matches the code
```

## Running locally

**Prerequisites:** .NET 10 SDK, SQL Server (the default connection string expects `localhost\SQLEXPRESS` with Windows authentication), and, only for adding migrations, the EF Core CLI (`dotnet tool install --global dotnet-ef`).

1. Check `ConnectionStrings:DefaultConnection` in `FromTheKitchen_API/appsettings.json` matches your SQL Server.
2. Set the JWT signing key. It isn't in the repo; it's stored in [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) on your machine. Use any random string of at least 32 bytes:
   ```bash
   dotnet user-secrets set "ApiSettings:Secret" "<random key, at least 32 bytes>" --project FromTheKitchen_API
   ```
   The API refuses to start without it. Outside development, set the `ApiSettings__Secret` environment variable instead.
3. Optional: choose an Admin account, which you need for the admin pages. The API creates it at startup. The password must meet Identity's rules: 6+ characters with upper case, lower case, a digit and a symbol.
   ```bash
   dotnet user-secrets set "SeedAdmin:Email" "<admin email>" --project FromTheKitchen_API
   dotnet user-secrets set "SeedAdmin:Password" "<admin password>" --project FromTheKitchen_API
   ```
4. Run the API. On startup it creates the database (applying migrations and the menu seed) if needed:
   ```bash
   dotnet run --project FromTheKitchen_API --launch-profile https
   ```
5. Open `https://localhost:7145/scalar/v1` to explore the API. Plain HTTP runs on `http://localhost:5077`.
6. Start the frontend in a second terminal:
   ```bash
   cd FromTheKitchen_Web
   npm install
   npm run dev
   ```
   It runs on `http://localhost:5173` and calls the API at `https://localhost:7145`. Set `VITE_API_URL` to point it somewhere else. Production builds call the API on their own origin, because the API serves them (see Deploying).

## Tests

```bash
dotnet test
```

60 integration tests run in about 5 seconds and need no setup: no SQL Server, no user secrets.

**How they work.** Each test class starts the real API in memory with `WebApplicationFactory<Program>`, so requests go through the same `Program.cs`, routing, model binding, Identity and JWT validation as in production. Only two things are swapped:
- SQL Server becomes a private SQLite in-memory database, created from the EF Core model, menu seed included.
- The web root and the upload folder become temporary folders, so test images never touch `wwwroot`.

Tests talk to the API over HTTP like a real client would. They register and log in real users and send real JWTs.

**What they cover:**

| Area | Examples |
| --- | --- |
| Authentication | Register and log in, JWT claims and 7-day expiry, wrong password, weak password, tampered token |
| Admin accounts | The first Admin is seeded from config (safe on restart, promotes an existing user only with the right password, fails fast on a weak password), nobody can self-register as Admin, Admins can create Admins |
| Menu | Public reads, 400 and 404 for bad ids, customers can't change the menu, Admin create, update (with or without a new image) and delete including the image file and serving it, unique upload names, only image types accepted, ordered items can't be deleted |
| Order pricing | Prices, names and totals come from the menu even when the request sends others; unknown items and quantities below 1 are rejected |
| Order ownership | The owner comes from the token not the body, customers see only their own orders, admins see all, 403 for someone else's order |
| Status workflow | One data-driven test covering every allowed and blocked change. A blocked change returns 400 and saves nothing. |
| Hosting | `/health` answers, the frontend origin gets CORS permission and other origins don't, frontend routes get `index.html` while unknown `/api` paths and missing files stay 404 |
| Ratings | Only the customer who placed the order can rate it, not other customers and not admins |
| API spec | The committed `docs/api/openapi.json` matches the OpenAPI document the API generates |

**Tradeoff:** SQLite is not SQL Server. SQL-Server-specific behaviour, like the migrations themselves, isn't covered. The next step would be running the same tests against a real SQL Server in Docker with [Testcontainers](https://dotnet.testcontainers.org/).

## Deploying (Azure)

The app runs on **Azure App Service** (Linux) with an **Azure SQL Database**. The API also serves the frontend, so the site and the API share one address and no CORS setup is needed.

[GitHub Actions](.github/workflows/ci-cd.yml) runs the tests on every push and pull request that changes the API or the frontend. Pushes to `main` then publish the API, build the frontend, copy it into the published `wwwroot`, and deploy both to App Service. Routes the frontend owns, like `/cart` or `/my-orders`, get `index.html` from the API, so refreshing a page or opening a shared link works. Unknown `/api` paths still return 404. [web.yml](.github/workflows/web.yml) also lints and builds frontend changes.

The app sets itself up on startup: it applies migrations to the empty database, seeds the menu, and creates the first Admin from settings. No manual database step is needed.

**One-time setup in the Azure portal:**

1. Create a resource group with:
   - an **Azure SQL Database** using the free offer. Under its server's Networking, allow Azure services to access it.
   - an **App Service** on Linux with runtime .NET 10. The Free (F1) plan is enough to start.
2. In the App Service, under **Settings → Environment variables**, add:

   | Name | Value |
   | --- | --- |
   | `ConnectionStrings__DefaultConnection` | The ADO.NET connection string from the SQL database page, with the password filled in |
   | `ApiSettings__Secret` | A new random key of at least 32 bytes (not the one you use locally) |
   | `SeedAdmin__Email` / `SeedAdmin__Password` / `SeedAdmin__Name` | The first Admin account |
   | `Cors__AllowedOrigins__0` | Only if a frontend on another address calls the API. Not needed for the frontend the API serves itself |
   | `ImageStorage__Path` | `/home/data/images`, which keeps uploaded images across restarts and redeploys |

3. Under **Monitoring → Health check**, set the path to `/health`.
4. Connect GitHub:
   - Under **Configuration → General settings**, turn on *SCM Basic Auth Publishing Credentials*. It's off by default and the publish profile needs it.
   - Download the **publish profile** from the Overview page.
   - In the GitHub repository, go to **Settings → Secrets and variables → Actions**. Add the secret `AZURE_WEBAPP_PUBLISH_PROFILE` (paste the file's contents) and the variable `AZURE_WEBAPP_NAME` (the App Service name).
5. Push to `main`, or re-run the workflow. Until the variable exists, the deploy job is skipped and only the tests run.

**Checking a deploy:**
- `https://<app>.azurewebsites.net/health` returns `Healthy`.
- `GET /api/MenuItem` lists the seeded dishes, which shows the migrations and seed ran.
- Logging in as the seed Admin returns a token with the `Admin` role.
- For a full end-to-end check, run [`scripts/live-smoke.ps1`](scripts/live-smoke.ps1) `-AdminEmail <seed admin email>`. It asks for the Admin password, then creates a throwaway customer and walks one order through the whole workflow (ownership, status rules, ratings), printing PASS/FAIL for each check. The test customer and its order stay in the database.

Once the Admin exists you can delete `SeedAdmin__Password`; the seeder skips when it's empty.

**Limits of this setup:**
- Migrations run at startup, which is fine for one instance. Before scaling out, run them as a pipeline step instead (an EF Core migration bundle).
- The free App Service plan sleeps when idle, so the first request after a while is slow.

## Status and next steps

This is a learning project, still in progress.

**Working today:**
- The full ordering flow, end to end, live on Azure.
- 60 integration tests in CI.
- An end-to-end smoke test (`scripts/live-smoke.ps1`) that passes against the live deployment.
- Design docs in [docs/](docs/README.md).

**Known gaps and next steps:**
- **Rating in the UI:** the API supports rating order items, but the frontend doesn't offer it yet.
- **Retiring dishes:** items that were ordered can't be deleted. An "available" flag on `MenuItem` (or a nullable `OrderDetail.MenuItemId`) would let admins hide them.
- **Schema hardening:**
  - Prices are stored as `float`; `decimal(10,2)` would avoid rounding errors.
  - `OrderDetails.MenuItemId` should use `ON DELETE RESTRICT` instead of cascade.
  - See the [data model notes](docs/er-diagram.md#notes).
- **Response types in the API spec:** `ApiResponse.result` is typed `object`, so the OpenAPI spec doesn't describe what each endpoint returns. Generic `ApiResponse<T>` types would fix that ([ADR 0008](docs/adr/0008-uniform-api-response-envelope.md)).
- **Ideas:** payments, a CDN in front of the static files, OIDC instead of a publish profile for deploys, and running the tests against real SQL Server with Testcontainers.
