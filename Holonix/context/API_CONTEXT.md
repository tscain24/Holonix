# Holonix API Context

Use this when a task changes request/response shapes, API routes, frontend services, auth behavior, or error handling.

## API Surface

- Backend controllers live in `Server/Controllers/`.
- Backend DTO records live under `Server/Contracts/`.
- Angular API clients live under `ClientApp/src/app/core/services/`.
- Angular DTO interfaces are currently declared inside the service files, not in a separate generated client.
- Angular dev proxy maps `/api` to `http://localhost:5237`.

## Controllers

- `AuthController`: `/api/auth`
  - `POST /register`
  - `POST /login`
  - `POST /refresh`
  - `GET /profile`
  - `PUT /profile`
  - `PUT /profile-image`
  - `DELETE /profile`
- `BusinessController`: `/api/business`; see `context/BUSINESS_CONTEXT.md` and `context/server/BUSINESS_CONTEXT.md`.
- `HomeController`: home/pending-invite data for the signed-in user.
- `WeatherForecastController`: scaffold/sample endpoint; avoid using as a pattern for domain work.

## Contract Alignment

When changing a backend DTO, update the corresponding Angular interface and service method:

- `Server/Contracts/Auth/*` -> `ClientApp/src/app/core/services/auth.service.ts`
- `Server/Contracts/Business/*` -> `ClientApp/src/app/core/services/business.service.ts`
- `Server/Contracts/Home/*` -> `ClientApp/src/app/core/services/home.service.ts`

Keep property names camelCase on the frontend; ASP.NET Core JSON serialization maps C# PascalCase records to camelCase by default.

## Error Shape

Most domain validation/auth errors use:

```json
{ "errors": ["Message text."] }
```

Frontend components generally extract `err.error.errors` and show the first message in a snack bar. Preserve this shape for new validation failures unless there is a strong reason to introduce another error model.

## Authenticated Requests

- Protected Angular requests are `/api/**` and localhost absolute `/api/**` URLs.
- Login, register, and refresh are intentionally bypassed by the auth interceptor.
- Authenticated backend endpoints should use `[Authorize]`.
- Current user id is read from `ClaimTypes.NameIdentifier`, falling back to `"sub"`.

## Local-Origin Fallbacks

Some Angular services retry direct backend origins when proxy calls return status `0` or `404`. Existing fallback lists are intentionally local-dev oriented and include ports such as `5237`, `7241`, `5000`, `44367`, and `33823`.

Do not remove fallback behavior as incidental cleanup; it exists to tolerate backend/frontend launch differences during development.
