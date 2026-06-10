# Holonix Context Docs

These files are lightweight, high-signal maps of the codebase meant to reduce repeated exploration (and token usage) during Codex runs.

Start with:

- `context/BUSINESS_CONTEXT.md`
- `context/API_CONTEXT.md`
- `context/AUTH_CONTEXT.md`

Backend-specific:

- `context/server/BUSINESS_CONTEXT.md`
- `context/server/DATA_CONTEXT.md`

Frontend-specific:

- `context/client/FRONTEND_CONTEXT.md`
- `context/client/business/BUSINESS_CONTEXT.md`

## Update Rule

When code changes affect behavior, contracts, routes, startup/runtime setup, or domain workflows, update the matching context file in the same change.

Use the ownership map below as the default source of truth.

## Ownership Map

- `start-dev.ps1`, `stop-dev.ps1`, repo-level dev/runtime behavior
  - Update: `AGENTS.md`
- `Server/Program.cs`, backend startup wiring, auth setup, CORS, seeders, runtime integrations
  - Update: `Server/AGENTS.md`, `context/API_CONTEXT.md`, `context/AUTH_CONTEXT.md`
- `Server/Controllers/AuthController.cs`, `Server/Application/Handlers/Auth/*`, token/session behavior
  - Update: `context/API_CONTEXT.md`, `context/AUTH_CONTEXT.md`
- `Server/Controllers/BusinessController.cs`, business contracts, business permissions, public business profile
  - Update: `context/API_CONTEXT.md`, `context/BUSINESS_CONTEXT.md`, `context/server/BUSINESS_CONTEXT.md`
- `Server/Controllers/HomeController.cs`
  - Update: `context/API_CONTEXT.md`
- `Server/Controllers/ServiceSearchController.cs`, `Server/Controllers/MapboxController.cs`, `Server/Controllers/GeocodingController.cs`, `Server/Controllers/DevCategoryEmbeddingsController.cs`
  - Update: `context/API_CONTEXT.md`, `context/client/FRONTEND_CONTEXT.md`
- `Server/Infrastructure/Data/*`, `Server/Domain/Entities/*`, migrations, seeders, database mappings
  - Update: `Server/AGENTS.md`, `context/server/DATA_CONTEXT.md`
- `ClientApp/src/app/app-routing.module.ts`, app-wide route structure
  - Update: `ClientApp/AGENTS.md`, `context/client/FRONTEND_CONTEXT.md`
- `ClientApp/src/app/core/services/auth*.ts`
  - Update: `context/API_CONTEXT.md`, `context/AUTH_CONTEXT.md`, `context/client/FRONTEND_CONTEXT.md`
- `ClientApp/src/app/core/services/business.service.ts`, business DTOs/API usage
  - Update: `context/API_CONTEXT.md`, `context/BUSINESS_CONTEXT.md`, `context/client/business/BUSINESS_CONTEXT.md`
- `ClientApp/src/app/core/services/home.service.ts`
  - Update: `context/API_CONTEXT.md`, `context/client/FRONTEND_CONTEXT.md`
- `ClientApp/src/app/core/services/service-search.service.ts`, `public-business.service.ts`, `mapbox-config.service.ts`, `search-origin.service.ts`
  - Update: `context/API_CONTEXT.md`, `context/client/FRONTEND_CONTEXT.md`
- `ClientApp/src/app/features/business/*`
  - Update: `context/BUSINESS_CONTEXT.md`, `context/client/business/BUSINESS_CONTEXT.md`
- `ClientApp/src/app/features/auth/*`, `ClientApp/src/app/features/profile/*`
  - Update: `context/AUTH_CONTEXT.md`, `context/client/FRONTEND_CONTEXT.md`
- `ClientApp/src/app/features/search/*`, search UX, public-profile navigation
  - Update: `context/client/FRONTEND_CONTEXT.md`, `context/client/business/BUSINESS_CONTEXT.md`, `context/BUSINESS_CONTEXT.md`

## Verification

Run `.\check-context.ps1` from the repo root before closing substantive code changes. The script flags changed code areas whose mapped context files were not also changed.
