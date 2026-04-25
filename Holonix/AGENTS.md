# Holonix Agent Context

Use this file as the first stop for Codex sessions. It is intentionally compact so agents do not need to rediscover the project shape on every turn.

## Project Shape

- `Server/`: ASP.NET Core Web API targeting `net10.0`.
- `ClientApp/`: Angular 16 app using Angular Material, RxJS, Tailwind/PostCSS, and the Angular CLI.
- Development entry point is the repo root PowerShell script `.\start-dev.ps1`; clean shutdown is `.\stop-dev.ps1`.
- Expected local URLs: frontend `http://localhost:4200`, backend HTTP `http://localhost:5237`, backend HTTPS `https://localhost:7241`, Swagger `http://localhost:5237/swagger`.
- Angular dev server proxies `/api` to `http://localhost:5237` through `ClientApp/proxy.conf.json`.

## Commands

- Start full dev stack: `.\start-dev.ps1`
- Stop full dev stack: `.\stop-dev.ps1`
- Backend build: `dotnet build .\Holonix.sln`
- Frontend build: from `ClientApp/`, `npm run build`
- Frontend unit tests: from `ClientApp/`, `npm test`

The Git repo may trip `safe.directory` checks because the parent directory ownership differs. Prefer one-off commands such as:

```powershell
git -c safe.directory=C:/Users/tscai/source/repos status --short
```

## Runtime Notes

- `start-dev.ps1` stages backend output into `.dev-runtime/server-run` and writes logs under `.dev-runtime/logs`.
- Do not place temporary verification directories under `Server/`. ASP.NET SDK content globbing can pull them into build output. The project excludes `tmp-*` paths, but keep temp artifacts outside source when possible.
- Existing `Server/tmp-*.log` files are local verification logs, not application source.
- Network access may be restricted during Codex runs; avoid dependency installation unless it is actually needed.

## Codebase Conventions

- Keep edits scoped to the requested feature or bug. Avoid broad cleanup in generated migration files or Angular scaffold files.
- Backend contracts live under `Server/Contracts/*`; Angular API DTO interfaces currently live mostly in `ClientApp/src/app/core/services/*.service.ts`.
- Backend controllers return `{ errors: string[] }` for many validation and authorization failures.
- Soft-delete/inactivation is common: users and businesses use `InactiveDate`; business memberships and roles use `IsActive`, `StartDate`, and `EndDate`.
- Business routes use a public `businessCode` where possible, with some server fallback support for legacy numeric ids.

## More Specific Context

- API contract/auth guidance: `context/API_CONTEXT.md`
- Auth/session guidance: `context/AUTH_CONTEXT.md`
- Backend-specific guidance: `Server/AGENTS.md`
- Frontend-specific guidance: `ClientApp/AGENTS.md`
- Business domain guidance: `context/BUSINESS_CONTEXT.md`
- Temporary artifact background: `Server/TMP_ARTIFACTS_CONTEXT.md`
