# 8. One response envelope for every endpoint

- **Status:** Accepted
- **Date:** 2026-11-19

## Context

The frontend needs one way to read results and show errors. ASP.NET Core can return bare objects, `ProblemDetails` for errors, or validation dictionaries, each in a different shape.

## Decision

Every endpoint returns an `ApiResponse`, except `/health` and the `AuthTest` endpoints:

```json
{ "statusCode": 200, "isSuccess": true, "errorMessages": [], "result": { } }
```

- **Status:** the HTTP status code matches `statusCode`.
- **Errors:** messages go in `errorMessages` as plain strings that the UI can show as they are.
- **Payload:** the data goes in `result`.

## Consequences

- **The frontend reads every call the same way:** `if (!isSuccess) show(errorMessages)`.
- **Not the standard error format.** `ProblemDetails` (RFC 9457) is what generic HTTP tools and clients expect, so they don't recognise these errors.
- **The OpenAPI spec is vague.** `result` is typed as `object`, so the spec can't describe each endpoint's payload. Generic `ApiResponse<T>` types would fix that.
- **Status codes are kept in two places,** the HTTP status and `statusCode`, so every action has to set both and keep them in agreement.
