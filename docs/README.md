# Documentation

| Document | What it covers |
| --- | --- |
| [Data model](er-diagram.md) | ER diagram of the database (Mermaid, rendered by GitHub), plus notes on delete behaviour and money types |
| [Architecture decisions](adr/README.md) | Why the API is built the way it is: auth, database, Admin seeding, order rules, image storage, hosting, testing, response format |
| [API spec](api/openapi.json) | OpenAPI 3.1 document for every endpoint, generated from the code |

## The API spec

`api/openapi.json` is produced by the running app (ASP.NET Core's built-in OpenAPI support), not written by hand. The test `OpenApiDocumentTests` regenerates it on every `dotnet test` and fails if the committed file differs, so it can't drift from the code.

**After changing an endpoint or DTO,** refresh the file and commit it:

```bash
UPDATE_OPENAPI=1 dotnet test --filter OpenApiDocumentTests
```

In PowerShell, run `$env:UPDATE_OPENAPI=1; dotnet test --filter OpenApiDocumentTests` instead, then `Remove-Item Env:UPDATE_OPENAPI`.

**To browse it:**
- **Interactive:** run the API locally and open `https://localhost:7145/scalar/v1`. You can log in and try every call there.
- **Without running anything:** paste the file into [editor.swagger.io](https://editor.swagger.io), or open it in an editor with an OpenAPI viewer.

**Not in the spec:**
- `GET /health`, the health check endpoint, because health checks aren't part of ASP.NET Core's API description.
- What's inside `result`. Every response uses the `ApiResponse` envelope, whose `result` field is typed as `object` ([ADR 0008](adr/0008-uniform-api-response-envelope.md)). The README's endpoint table describes what each call returns.
