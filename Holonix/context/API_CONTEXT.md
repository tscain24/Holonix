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
  - `POST /profile/locations`
  - `PUT /profile/locations/{userSavedLocationId}`
  - `DELETE /profile/locations/{userSavedLocationId}`
  - Profile location create/update requests must include a valid geocoded latitude/longitude pair; the Angular profile form resolves typed addresses through `/api/geocode/resolve-address` or Mapbox suggestions before saving.
  - `PUT /profile-image`
  - `DELETE /profile`
- `BusinessController`: `/api/business`; see `context/BUSINESS_CONTEXT.md` and `context/server/BUSINESS_CONTEXT.md`.
  - Includes `GET /public/{businessCode}` for the public business profile.
  - Includes `GET /public/{businessCode}/availability/daily` for public date-specific booking availability. That route accepts optional repeated `businessSubServiceIds` query values to make public slots service-aware.
  - Includes `GET /{businessCode}/my-availability/daily` and `GET /{businessCode}/availability/team/daily`.
  - The daily availability routes now share a backend availability calculator so public and workspace surfaces stay aligned, including all-day time off exclusion and partial-day time off subtraction.
  - Business sub-service create/update and workspace/public profile payloads now include `requiresServiceLocation` so customer booking flows can tell when a selected service needs a user location on a later step.
  - `POST /api/business` still uses the same address fields, but the create-business screen now runs a single location flow: `address1` and `zipCode` may be blank, while `city` and `state` remain required. When the request is effectively a broader location instead of a street address, the frontend resolves and submits latitude/longitude before launch.
- `HomeController`: `/api/home`
  - `GET /pending-invites`
  - `POST /pending-invites/{workloadId}/accept`
  - `POST /pending-invites/{workloadId}/deny`
- `ServiceSearchController`: `/api/search`
  - `GET /services`
  - `GET /businesses`
- `GeocodingController`: `/api/geocode`
  - `GET /address-suggestions`
  - `GET /location-suggestions`
  - `POST /resolve-address`
    - Accepts either a street-address query or a broader city/state/ZIP query. When `address1` is blank, the resolver falls back to location-style geocoding instead of rejecting the request as incomplete.
- `MapboxController`: `/api/mapbox`
  - `GET /public-token`
- `DevCategoryEmbeddingsController`: `/api/dev/category-embeddings`
  - `POST /generate`
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
