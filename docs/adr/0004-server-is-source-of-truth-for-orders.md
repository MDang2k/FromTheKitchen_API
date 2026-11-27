# 4. The server is the source of truth for orders

- **Status:** Accepted
- **Date:** 2026-11-19 (ownership and status rules); 2026-11-27 (server-side pricing)

## Context

The browser can't be trusted: anyone can edit a request in the dev tools. Early versions took the order's owner, line prices and totals straight from the request body, and accepted any status change.

## Decision

The API ignores or rejects anything in a request that would let a client decide something only the business should:

- **Owner:** `ApplicationUserId` comes from the JWT `id` claim. Any value in the body is ignored. Customers can only read or change their own orders (otherwise 403); Admins can read and change all of them.
- **Prices and totals:**
  - Line prices and names are looked up from `MenuItems` when the order is placed.
  - `OrderTotal` and `TotalItem` are computed from those lines.
  - An empty order, a quantity of 0 or less, or an unknown menu item returns 400.
  - The header and lines are saved in one `SaveChanges`, so a failure leaves nothing half-written.
- **Status workflow:** `Confirmed → Ready For Pick Up → Completed`, plus `Cancelled` from any state before `Completed`.
  - Only Admins move an order forward. Customers may only cancel.
  - Any other change returns 400 and saves nothing: skipping a step, reopening a cancelled order, or cancelling a completed one.
- **Ratings:** only the customer who placed the order can rate its lines. Admins can't either.

## Consequences

- **The frontend can be simple and untrusted.** It may show a total, but the server's total is what gets stored.
- **Integration tests cover every rule.** That includes a data-driven test for each status transition ([ADR 0007](0007-integration-tests-on-sqlite.md)), and `scripts/live-smoke.ps1` checks them against the deployed API.
- **Status is a plain string column with constants in `SD.cs`.** Adding a state means changing code, not data, which is fine for four states. An enum or a lookup table would add type safety.
- **Prices are `double`/`float`,** so totals can show rounding artifacts. Moving to `decimal` would fix that ([data model notes](../er-diagram.md#notes)).
