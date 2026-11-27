# 1. JWT bearer tokens for authentication

- **Status:** Accepted
- **Date:** 2026-11-19

## Context

The API is called by a separate React single-page app, which may be hosted on another origin. Users are stored with ASP.NET Core Identity. The API needs to know who the caller is and whether they are an `Admin` or a `Customer`, without keeping server-side sessions.

## Decision

- `POST /api/Auth/login` checks the password with Identity and returns a JWT signed with HMAC-SHA256.
- **Claims in the token:**
  - `id`: the user id;
  - `fullname`: the user's name;
  - `email`;
  - `role`: one role. Admin wins if a user has several.
- **Lifetime:** the token is valid for 7 days, with no clock skew.
- **Signing key:** comes from configuration (`ApiSettings:Secret`): user secrets locally, an App Service setting in Azure. The app refuses to start without it.
- **Sending it:** the frontend stores the token and sends it as `Authorization: Bearer <token>`. Endpoints use `[Authorize]` and `[Authorize(Roles = "Admin")]`.

## Consequences

- **Benefits:** stateless and simple. Any instance can check a token, and cookies, CSRF protection and session storage aren't needed.
- **No revocation:** a token stays valid until it expires, even after a password change or a role change. A role change only takes effect at the next login.
- **No refresh tokens:** users log in again after 7 days.
- **The token can be stolen.** The browser keeps it where JavaScript can read it, so an XSS bug in the frontend could steal it. Keeping the frontend free of injected HTML matters more because of this.
- **Single signing key:** rotating `ApiSettings:Secret` logs everyone out.
- **Upgrade path:** short-lived access tokens with refresh tokens, or an external identity provider (Microsoft Entra ID, Auth0), if revocation or SSO is ever needed.
