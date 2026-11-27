# 3. Seed the first Admin from configuration

- **Status:** Accepted
- **Date:** 2026-11-25
- **Supersedes:** the original rule, "the first person to register as Admin becomes Admin"

## Context

Admins manage the menu and move orders through their statuses. The original register endpoint let anyone ask for the `Admin` role while no Admin existed yet. On a public URL, a stranger could claim it before the owner did.

## Decision

- **No self-registration as Admin.** `POST /api/Auth/register` with `role: "Admin"` returns 403 unless the caller already has an Admin token.
- **`AdminSeeder` runs at startup** after the migrations:
  - It creates the `Admin` and `Customer` roles if they're missing.
  - If `SeedAdmin:Email` and `SeedAdmin:Password` are set, it creates that account as Admin.
- **Safe on restart:** an existing Admin is left alone.
- **Promotion needs the password:** an existing account with the seed email is only promoted if `SeedAdmin:Password` matches its password. Otherwise anyone who registered that email first would become Admin.
- **Fails loudly:** a seed password that breaks Identity's rules stops startup with Identity's error message, rather than starting without an Admin.

## Consequences

- **The owner controls who becomes Admin** through App Service settings or user secrets.
- **It doesn't reset passwords.** Changing `SeedAdmin:Password` doesn't change an existing Admin's password. To recover, set a new seed email.
- **The setting can go once used.** `SeedAdmin:Password` can be deleted after the first start; the seeder skips when it's empty.
- **Further Admins** are created by an existing Admin calling `register` with their token.
