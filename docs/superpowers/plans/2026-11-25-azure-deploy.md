# Azure Deployment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the API safe and ready to run on Azure App Service + Azure SQL, and deploy it from GitHub Actions on every push to `main`.

**Architecture:** The app prepares its own database at startup: it applies migrations and seeds the first Admin from configuration. Hosting config (allowed CORS origins, image folder, secrets) comes from App Service settings. Uploaded images go to a folder outside the deployed app (`/home/data/images` on App Service, which survives restarts and redeploys). A single workflow tests every push and deploys `main`.

**Tech Stack:** ASP.NET Core / .NET 10, EF Core 10 (SQL Server), ASP.NET Core Identity, xUnit + WebApplicationFactory (SQLite in-memory), GitHub Actions, Azure App Service (Linux), Azure SQL Database.

**Spec:** No separate document. The requirements are the five pre-deploy changes agreed in chat on 2026-11-25: first Admin from config, CORS, persistent image storage, migrations at startup, health check. Plus the CI/CD pipeline and the Azure setup.

## Global Constraints

- Every existing test keeps passing. `dotnet test` runs with no SQL Server and no user secrets.
- No new NuGet packages in the API project. Health checks, CORS and static files are built in.
- Secrets (JWT key, connection string, seed Admin password) only ever come from user secrets locally and App Service settings in Azure. Never put them in `appsettings*.json` or the workflow file.
- Config keys, exactly as written: `SeedAdmin:Email`, `SeedAdmin:Password`, `SeedAdmin:Name`, `Cors:AllowedOrigins` (string array), `ImageStorage:Path`.
- In App Service settings `:` becomes `__` (for example `SeedAdmin__Email`, `Cors__AllowedOrigins__0`).
- Local frontend origin: `http://localhost:5173` (Vite default; `FromTheKitchen/vite.config.ts` sets no port).
- Error responses keep the existing `ApiResponse` envelope.

## Review Focus

1. **Seed Admin password too weak for Identity's rules.** Startup must fail with the Identity error text, not quietly start with no Admin. Test in Task 1.
2. **App restarts after the Admin was already seeded.** The seeder must not throw and must not create a second user. Test in Task 1.
3. **The configured seed email already belongs to a Customer.** The user is promoted to Admin, not duplicated. Test in Task 1.
4. **An uploaded file name with path segments (`../../evil.jpg`).** The file must land inside the images folder. Today `File.FileName` goes straight into `Path.Combine`. Test in Task 3.
5. **A request from an origin that isn't allowed.** It gets no `Access-Control-Allow-Origin` header. Test in Task 2.

---

### Task 1: Database prep at startup and seeding the first Admin from config

Closes the "first person to register as Admin wins" hole before the API goes public.

**Files:**
- Create: `FromTheKitchen_API/Data/AdminSeeder.cs`
- Modify: `FromTheKitchen_API/Program.cs` (after `var app = builder.Build();`)
- Modify: `FromTheKitchen_API/Controllers/AuthController.cs:35-46` (Admin check), `:59-63` (role creation)
- Modify: `FromTheKitchen_API.Tests/Infrastructure/ApiFactory.cs` (seed settings, `GetAdminAsync`, remove the `CreateHost` override)
- Test: `FromTheKitchen_API.Tests/AuthTests.cs`, new `FromTheKitchen_API.Tests/AdminSeederTests.cs`
- Modify: `README.md` (Running locally step 2, "What it does" auth section, Status section)

**Interfaces:**
- Produces: `public static class AdminSeeder { public static Task SeedAsync(IServiceProvider services, IConfiguration config); }`
  - Always makes sure the `Admin` and `Customer` roles exist.
  - If `SeedAdmin:Email` or `SeedAdmin:Password` is empty, it stops there.
  - If no user has that email, it creates one (`UserName` = `Email` = the email, `Name` = `SeedAdmin:Name` or `"Admin"`). If `CreateAsync` fails, it throws `InvalidOperationException` with the Identity error descriptions.
  - If the user isn't in the `Admin` role, it adds them.
- Produces: `ApiFactory.SeedAdminEmail` and `ApiFactory.SeedAdminPassword` (public consts). `GetAdminAsync()` now calls `LoginAsync(SeedAdminEmail, SeedAdminPassword)` (still cached).

- [ ] **Step 1: Write the failing tests**

In `AuthTests.cs`, replace `The_first_admin_can_register_on_an_empty_database_but_the_next_one_cannot` with:
```csharp
[Fact]
public async Task Nobody_can_self_register_as_admin_even_on_a_fresh_database()
{
    using var freshApi = new ApiFactory();
    var response = await freshApi.CreateClient().PostAsJsonAsync("/api/Auth/register", ApiFactory.NewRegistration(SD.Role_Admin));
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task The_admin_from_configuration_can_log_in_with_the_admin_role()
{
    var admin = await factory.GetAdminAsync();
    Assert.Equal(SD.Role_Admin, admin.Role);
}
```
New `AdminSeederTests.cs`. Each test builds its own `ApiFactory` and resolves services from `factory.Services`:
- `Seeding_twice_keeps_a_single_admin`: call `AdminSeeder.SeedAsync` a second time, then assert `GetUsersInRoleAsync("Admin")` has `Count == 1`.
- `An_existing_customer_with_the_seed_email_is_promoted`: register a Customer, then call `SeedAsync` with an in-memory config whose `SeedAdmin:Email` is that customer's email (any password). Assert `IsInRoleAsync(user, "Admin")` is true and only one user has that email.
- `A_weak_seed_password_stops_startup`: run `SeedAsync` with a new email and password `"short"`. Assert it throws `InvalidOperationException` and the message contains `"Passwords must"`.

- [ ] **Step 2: Run the tests and check they fail**

Run: `dotnet test --filter "FullyQualifiedName~AdminSeeder|FullyQualifiedName~AuthTests"`
Expected: compile error, because `AdminSeeder` doesn't exist yet.

- [ ] **Step 3: Implement**

**`AdminSeeder.SeedAsync`:** follow the Interfaces block above.

**`Program.cs`:** after `Build()`, open a scope and run:
```csharp
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
// ponytail: tests run on SQLite, which can't apply SQL Server migrations. Migrating at startup
// is fine for one instance; move to a migration bundle in the pipeline before scaling out.
if (db.Database.IsSqlServer()) db.Database.Migrate(); else db.Database.EnsureCreated();
await AdminSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
```

**`AuthController.Register`:** `wantsAdmin && !User.IsInRole(SD.Role_Admin)` now always returns 403. Remove the bootstrap count check and its `ponytail:` comment. Also remove the role-creation block, since the seeder owns the roles now.

**`ApiFactory`:**
- Add `UseSetting` calls for the `SeedAdmin:*` keys.
- Delete the `CreateHost` override, because `Program.cs` creates the database now.
- Change `GetAdminAsync()` to log in as the seeded Admin.

**README:**
- Running locally, step 2: also set `SeedAdmin:Email` and `SeedAdmin:Password` in user secrets.
- Step 3 (`dotnet ef database update`) becomes optional.
- Delete the "First Admin" item from Status.

- [ ] **Step 4: Run the full suite**

Run: `dotnet test`
Expected: `Failed: 0`, 44 tests (40 − 1 replaced + 2 auth + 3 seeder).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Seed the first Admin from configuration and migrate the database at startup"
```

---

### Task 2: CORS for the frontend and a health check

**Files:**
- Modify: `FromTheKitchen_API/Program.cs`
- Modify: `FromTheKitchen_API/appsettings.Development.json`
- Modify: `FromTheKitchen_API.Tests/Infrastructure/ApiFactory.cs` (one more `UseSetting`)
- Test: new `FromTheKitchen_API.Tests/HostingTests.cs`

**Interfaces:**
- Produces: `GET /health`, which returns 200 with the body `Healthy`. App Service's health check points here.
- Produces: the default CORS policy. It allows the origins in `Cors:AllowedOrigins` with any header and any method, and no credentials, because the frontend sends a Bearer header rather than cookies.

- [ ] **Step 1: Write the failing tests** (`HostingTests : IClassFixture<ApiFactory>`; the factory sets `Cors:AllowedOrigins:0` = `https://frontend.example.test`)
  - `Health_endpoint_reports_healthy`: `GET /health` returns 200 with the body `"Healthy"`.
  - `The_frontend_origin_may_call_the_api`: an `OPTIONS /api/MenuItem` request with headers `Origin: https://frontend.example.test` and `Access-Control-Request-Method: GET` gets back `Access-Control-Allow-Origin` equal to that origin.
  - `Other_origins_get_no_cors_header`: the same request with `Origin: https://evil.example.test` has no `Access-Control-Allow-Origin` header.

- [ ] **Step 2: Run the tests and check they fail**

Run: `dotnet test --filter FullyQualifiedName~HostingTests`
Expected: FAIL, because `/health` returns 404 and the CORS header is missing.

- [ ] **Step 3: Implement**

**`Program.cs`:**
- `builder.Services.AddHealthChecks();` and `app.MapHealthChecks("/health");`
- `AddCors` with a default policy using `builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []`.
- `app.UseCors()` goes after `UseStaticFiles` and before `UseAuthentication`.

**`appsettings.Development.json`:** `"Cors": { "AllowedOrigins": [ "http://localhost:5173" ] }`.

- [ ] **Step 4: Run the full suite**

Run: `dotnet test`
Expected: `Failed: 0`

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add CORS for the frontend origin and a /health endpoint"
```

---

### Task 3: Store uploaded images outside the deployed app

On App Service, a deploy replaces the app folder, so uploads under `wwwroot/images` would be lost. `/home/data` is persistent storage shared by all instances. Seeded images stay in `wwwroot/images` (they ship with the app). Both folders are served under `/images`: the static file middleware tries `wwwroot` first and falls through to the uploads folder.

**Files:**
- Modify: `FromTheKitchen_API/Utility/SD.cs`
- Modify: `FromTheKitchen_API/Controllers/MenuItemController.cs:75-99` (create), `:155-181` (update), `:229-234` (delete)
- Modify: `FromTheKitchen_API/Program.cs` (second `UseStaticFiles`)
- Modify: `FromTheKitchen_API.Tests/Infrastructure/ApiFactory.cs`
- Test: `FromTheKitchen_API.Tests/MenuItemTests.cs`

**Interfaces:**
- Produces: `public static string ImagesFolder(IConfiguration config, IWebHostEnvironment env)` in `SD`. It returns `ImageStorage:Path` when that's set, otherwise `Path.Combine(env.WebRootPath, "images")`.
- Produces: a private `async Task<string> SaveImageAsync(IFormFile file)` in `MenuItemController`. It saves to `ImagesFolder/Path.GetFileName(file.FileName)`, creates the folder if needed, overwrites any existing file, and returns `"images/" + name`. Create and update both use it, which replaces the duplicated blocks and the double delete.
- Produces: `ApiFactory.ImagesPath`, a temp folder separate from `WebRoot`, set as `ImageStorage:Path` and deleted in `Dispose`.

- [ ] **Step 1: Write the failing tests** (`MenuItemTests.cs`)
  - Update `An_admin_can_create_update_and_delete_a_menu_item_with_its_image` to check files in `factory.ImagesPath` instead of `WebRoot/images`. Add, after create: `GET /images/{imageName}` returns 200 and the body bytes equal `[0xFF, 0xD8, 0xFF, 0xD9]`.
  - New `An_uploaded_file_name_cannot_escape_the_images_folder`: an Admin creates an item with the file name `"../../escape-{guid}.jpg"`. Assert 201, and that `File.Exists(Path.Combine(factory.ImagesPath, "escape-{guid}.jpg"))` is true, and `File.Exists(Path.GetFullPath(Path.Combine(factory.ImagesPath, "..", "..", "escape-{guid}.jpg")))` is false.
  - Existing `A_single_menu_item_can_be_fetched_by_id` stays as it is. Add one line: `GET` the seeded item's image (from `wwwroot`) returns 200, which proves the seeded images are still served.

- [ ] **Step 2: Run the tests and check they fail**

Run: `dotnet test --filter FullyQualifiedName~MenuItemTests`
Expected: FAIL, because files still land in `WebRoot/images` and the escape test writes outside the folder.

- [ ] **Step 3: Implement**

**`SD.ImagesFolder` and `SaveImageAsync`:** as in the Interfaces block.

**Delete action:** delete `Path.Combine(ImagesFolder, Path.GetFileName(item.Image))` if it exists.
```csharp
// ponytail: seeded images live in wwwroot and are restored by every deploy, so they are left alone
```

**`Program.cs`:** after the existing `UseStaticFiles()`, add:
```csharp
UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(folder), RequestPath = "/images" })
```
- `folder` = `SD.ImagesFolder(...)`. Create it first with `Directory.CreateDirectory`.
- Skip this when it equals the `wwwroot/images` path.

- [ ] **Step 4: Run the full suite**

Run: `dotnet test`
Expected: `Failed: 0`

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Save uploaded images to a configurable folder outside the deployed app"
```

---

### Task 4: GitHub Actions: test every push, deploy main

**Files:**
- Create: `.github/workflows/ci-cd.yml`
- Modify: `README.md` (status badge under the title)

**Interfaces:**
- Consumes these GitHub settings, which you create in Task 5:
  - Repository **variable** `AZURE_WEBAPP_NAME`.
  - Repository **secret** `AZURE_WEBAPP_PUBLISH_PROFILE`.
- **Jobs:**
  - `test`: runs on every push and pull request. Steps: checkout, set up .NET `10.0.x`, then `dotnet test --configuration Release`.
  - `deploy`: `needs: test`. Runs only when `github.ref == 'refs/heads/main' && github.event_name == 'push' && vars.AZURE_WEBAPP_NAME != ''`, so pushes keep passing before Azure exists. It runs `dotnet publish FromTheKitchen_API -c Release -o publish`, then `azure/webapps-deploy@v3` with `app-name: ${{ vars.AZURE_WEBAPP_NAME }}`, `publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}` and `package: publish`.
- Action versions: `actions/checkout@v4`, `actions/setup-dotnet@v4`, `azure/webapps-deploy@v3`. Check that each is still the current major version when implementing.

- [ ] **Step 1: Write the workflow and the badge**

Badge: `[![CI/CD](https://github.com/MDang2k/FromTheKitchen_API/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/MDang2k/FromTheKitchen_API/actions/workflows/ci-cd.yml)`

- [ ] **Step 2: Validate locally**

Run: `dotnet test --configuration Release` and `dotnet publish FromTheKitchen_API -c Release -o publish`, the two commands the workflow runs.
Expected: both succeed. `publish/` contains `FromTheKitchen_API.dll` and `wwwroot/images/*.jpg`. Delete `publish/` afterwards; check it's in `.gitignore` or add it.

- [ ] **Step 3: Commit and push** (only when the user asks to push)

```bash
git add -A
git commit -m "Add GitHub Actions workflow: test every push, deploy main to Azure"
git push
```
Expected: on GitHub, the `test` job passes and `deploy` is skipped.

---

### Task 5: Azure setup and first deploy (manual, you do this in the Azure portal)

Creating the account and resources needs your sign-in and acceptance of Azure's terms. This task is a checklist, written into the README as a **Deploying** section so it stays with the code.

- [ ] **Step 1: Create the resources.** One resource group in one region:
  - Azure SQL Database using the **free offer** (serverless). Under Networking, allow Azure services to access the server.
  - App Service on **Linux**, runtime **.NET 10**, Free (F1) plan to start.
- [ ] **Step 2: App Service → Environment variables.** Add:
  - `ConnectionStrings__DefaultConnection`: the ADO.NET string from the SQL database page, with your SQL password filled in.
  - `ApiSettings__Secret`: a new random key of at least 32 bytes. Don't reuse your local one.
  - `SeedAdmin__Email`, `SeedAdmin__Password`, `SeedAdmin__Name`.
  - `Cors__AllowedOrigins__0`: the frontend URL, once it's deployed.
  - `ImageStorage__Path`: `/home/data/images`
- [ ] **Step 3: App Service → Health check.** Set the path to `/health`.
- [ ] **Step 4: Connect GitHub.**
  - App Service → Configuration → General settings: turn on **SCM Basic Auth Publishing Credentials**. New apps have it off, and the publish profile needs it.
  - Download the publish profile.
  - In the GitHub repo, go to Settings → Secrets and variables → Actions. Add the secret `AZURE_WEBAPP_PUBLISH_PROFILE` (the file's contents) and the variable `AZURE_WEBAPP_NAME`.
- [ ] **Step 5: Deploy and verify.** Re-run the workflow, or push.
  - `https://<app>.azurewebsites.net/health` returns `Healthy`.
  - `GET /api/MenuItem` lists the 10 seeded dishes, which means migrations and the seed ran.
  - `POST /api/Auth/login` with the seed Admin returns a token with the `Admin` role.
  - Admin uploads a menu item. After redeploying, its image URL still loads.
- [ ] **Step 6: Optional.** Remove `SeedAdmin__Password` from App Service once the Admin exists. The seeder skips when it's empty.

**Out of scope for this plan:**
- Deploying the frontend (Azure Static Web Apps or Vercel). Its URL goes into `Cors__AllowedOrigins__0`.
- Order totals calculated on the server.
- OIDC login for Actions instead of a publish profile.
