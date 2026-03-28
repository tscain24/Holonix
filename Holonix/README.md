# Holonix

Minimal ASP.NET Core Web API + Angular (ClientApp) starter scaffold.

## Structure
- Server: `Server` — ASP.NET Core Web API (net8.0)
- Client: `ClientApp` — Angular app (created with @angular/cli v16)

## Requirements
- .NET 8+ SDK
- Node.js 20.x (upgrade to >=20.19 for latest Angular CLI)
- npm

## Run (development)
Preferred start from the repo root:

```powershell
.\start-dev.ps1
```

This script:
- stops previously recorded dev processes,
- clears listeners on ports `4200`, `5237`, and `7241`,
- starts the backend first and waits for Swagger,
- starts the frontend second and waits for `http://localhost:4200`,
- writes logs under `.dev-runtime/logs`.

Expected development URLs:
- Frontend: `http://localhost:4200`
- Backend HTTP: `http://localhost:5237`
- Backend HTTPS: `https://localhost:7241`
- Swagger: `http://localhost:5237/swagger`

To stop everything cleanly from the repo root:

```powershell
.\stop-dev.ps1
```

If you need to run the services manually instead, use:

Backend:

```powershell
cd Server
dotnet run
```

Frontend:

```powershell
cd ClientApp
npm install
npx ng serve --open
```

## Build (production)
Build backend solution:

```powershell
cd <repo-root>
dotnet build .\Holonix.sln
```

Build frontend production bundle:

```powershell
cd ClientApp
npx ng build --configuration=production
```

## Notes
- The `dotnet new angular` template was not available with the installed .NET templates, so this project uses a standalone Angular app created with the Angular CLI.
- If you want the latest Angular CLI features, upgrade Node.js to >=20.19 or install a compatible Node version manager.
- The Angular dev server proxies `/api` requests to the backend.



