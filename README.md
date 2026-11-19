# From The Kitchen API

Backend for **From The Kitchen**, a small restaurant ordering app. Customers browse a menu, place pickup orders, and track them. Admins manage the menu.

This is a personal project. I built it to practise backend development with C# and ASP.NET Core: REST API design, Entity Framework Core, ASP.NET Core Identity, JWT authentication and role-based authorization.

The frontend is a separate project (React 19 + TypeScript + Vite + Tailwind CSS) in the sibling `FromTheKitchen` folder. It calls this API.

## Tech stack

| Area | Technology |
| --- | --- |
| Framework | ASP.NET Core Web API on **.NET 10** |
| Data access | Entity Framework Core 10 (code-first, migrations) |
| Database | SQL Server (SQL Express locally) |
| Users and roles | ASP.NET Core Identity (`IdentityDbContext<ApplicationUser>`) |
| Authentication | JWT Bearer tokens (`System.IdentityModel.Tokens.Jwt`) |
| API docs | Built-in OpenAPI document + [Scalar](https://github.com/scalar/scalar) UI |
| File storage | Menu images saved to `wwwroot/images` and served as static files |

## What it does

### Authentication and roles
- `POST /api/Auth/register` creates a user with ASP.NET Core Identity. The first registration also creates the `Admin` and `Customer` roles. New users are `Customer` by default. Asking for `Admin` only works when the request carries an Admin's JWT, or when no Admin exists yet, so the first Admin of a fresh database can sign up. Otherwise it returns 403.
- `POST /api/Auth/login` checks the password and returns a JWT (valid 7 days) holding the user's id, email and role, plus the email and role in the response body.
- The API validates the token's signature and lifetime on every request that needs authorization. `[Authorize(Roles = "Admin")]` limits endpoints to admins.

### Menu
- The database is seeded with 10 dishes (appetizers, salads, entrees, desserts, beverages), each with a price, a category, an optional tag such as "Best Seller" or "Chef's Special", and an image.
- Menu items are created and updated with `multipart/form-data`, so the image file is uploaded in the same request and saved under `wwwroot/images`.

### Orders
- An order is an `OrderHeader` (pickup name, phone, email, user, total, item count, status, date) with a list of `OrderDetail` lines (menu item, quantity, name and price at time of order).
- Orders can be listed (newest first) or fetched one by one with their lines and menu items.
- Pickup details (name, phone, email) can be updated. Only fields that are sent are changed.
- Status follows a simple workflow. New orders start as `Confirmed`, then move to `Ready For Pick Up`, then `Completed`. An order can be `Cancelled` until it is completed. Any other change, such as skipping a step or reopening a cancelled order, returns 400 and saves nothing.
- Customers can rate each item of their own orders (`Rating` on `OrderDetail`).

### Who can do what with orders
- **Customers** only see and update their own orders. The order's owner comes from the JWT, not the request body, so nobody can place an order for someone else. Customers can edit pickup details and cancel their own orders.
- **Admins** see every order (optionally filtered with `?userId=`) and are the only ones who move an order to `Ready For Pick Up` or `Completed`.
- Touching someone else's order returns 403.

### Consistent responses
Every endpoint except the test ones wraps its result in the same `ApiResponse` shape, so the frontend handles every call the same way:

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
| PUT | `/api/MenuItem?id={id}` | Admin | Update a menu item and replace its image (form-data) |
| DELETE | `/api/MenuItem?id={id}` | Admin | Delete a menu item and its image |
| GET | `/api/OrderHeader?userId={id}` | Logged in | List orders: customers get their own, admins get all or one user's |
| GET | `/api/OrderHeader/{orderId}` | Owner or Admin | Get one order |
| POST | `/api/OrderHeader` | Logged in | Place an order for yourself |
| PUT | `/api/OrderHeader/{orderId}` | Owner or Admin | Update pickup details or cancel (owner); move to the next status (Admin) |
| PUT | `/api/OrderDetails/{orderDetailId}` | Owner | Rate one item of your order |
| GET | `/api/AuthTest` | Any logged-in user | Checks that a JWT works |
| GET | `/api/AuthTest/{value}` | Admin | Checks that the Admin role works |

In development the full interactive reference is at `/scalar/v1`, where you can paste a JWT and try every endpoint.

## Project structure

```
FromTheKitchen_API/
├── FromTheKitchen_API.slnx
└── FromTheKitchen_API/
    ├── Program.cs              # Services: EF Core, Identity, JWT, OpenAPI/Scalar, static files
    ├── appsettings.json        # Connection string (the JWT secret lives in user secrets)
    ├── Controllers/
    │   ├── AuthController.cs       # Register / login, issues JWTs
    │   ├── MenuItemController.cs   # Menu CRUD and image upload
    │   ├── OrderHeaderController.cs  # Place, list, update orders
    │   ├── OrderDetailsController.cs # Rate order items
    │   └── AuthTestController.cs   # Endpoints for testing auth and roles
    ├── Data/
    │   └── ApplicationDbContext.cs # DbSets and menu seed data
    ├── Models/
    │   ├── ApplicationUser.cs, MenuItem.cs, OrderHeader.cs, OrderDetail.cs
    │   ├── ApiResponse.cs          # Shared response wrapper
    │   └── Dto/                    # Request/response DTOs
    ├── Migrations/             # EF Core migrations (Identity, menu, seed, orders, ratings)
    ├── Utility/SD.cs           # Role names and order status constants
    └── wwwroot/images/         # Menu item images
```

## Running locally

**Prerequisites:** .NET 10 SDK, SQL Server (the default connection string expects `localhost\SQLEXPRESS` with Windows authentication), and the EF Core CLI (`dotnet tool install --global dotnet-ef`).

1. Check `ConnectionStrings:DefaultConnection` in `FromTheKitchen_API/appsettings.json` matches your SQL Server.
2. Set the JWT signing key. It isn't in the repo; it's stored in [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) on your machine. Use any random string of at least 32 bytes:
   ```bash
   dotnet user-secrets set "ApiSettings:Secret" "<random key, at least 32 bytes>" --project FromTheKitchen_API
   ```
   The API refuses to start without it. Outside development, set the `ApiSettings__Secret` environment variable instead.
3. Create the database and seed the menu:
   ```bash
   dotnet ef database update --project FromTheKitchen_API
   ```
4. Run the API:
   ```bash
   dotnet run --project FromTheKitchen_API --launch-profile https
   ```
5. Open `https://localhost:7145/scalar/v1` to explore the API. Plain HTTP runs on `http://localhost:5077`.
6. Start the frontend from the `FromTheKitchen` folder with `npm install` and `npm run dev`.

## Status and next steps

This is a learning project, still in progress:

- **First Admin:** on a fresh database, whoever registers as Admin first becomes Admin. That's fine locally. Before any deployment, seed the first Admin from configuration instead.
- **Order totals come from the client:** `OrderTotal`, `TotalItem` and line prices are taken from the request as sent. They should be worked out on the server from the menu prices.
- **Ideas:** payments and automated tests.
