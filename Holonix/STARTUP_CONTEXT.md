# Holonix Startup Context

Use this file when you want to bring up the full development environment quickly.

## Fast Path

From the repo root, run:

```powershell
.\start-dev.ps1
```

That script now:

- stops previously recorded dev processes,
- clears listeners on ports `4200`, `5237`, and `7241`,
- starts the compiled backend executable when present,
- falls back to a backend build only if the executable is missing,
- starts the backend first and waits for Swagger to respond,
- starts the frontend second and waits for `http://localhost:4200`,
- stores PIDs under `.dev-runtime`.

It writes logs to:

- `.dev-runtime/logs/server-<timestamp>.log`
- `.dev-runtime/logs/server-<timestamp>.err.log`
- `.dev-runtime/logs/client-<timestamp>.log`
- `.dev-runtime/logs/client-<timestamp>.err.log`

The latest log paths are also recorded in `.dev-runtime/latest-logs.txt`.

To stop everything cleanly:

```powershell
.\stop-dev.ps1
```

## App Layout

- Backend: `Server` (`ASP.NET Core`, `dotnet run`)
- Frontend: `ClientApp` (`Angular`, `npm run start`)

## Expected Dev URLs

- Frontend app: `http://localhost:4200`
- Backend API: `http://localhost:5237`
- Backend Swagger: `http://localhost:5237/swagger`
- Backend HTTPS: `https://localhost:7241`

The Angular dev server proxies `/api` requests to `http://localhost:5237` via `ClientApp/proxy.conf.json`.

## One-Time Checks

- `.NET 8 SDK` installed
- `Node.js` installed
- `npm` available
- `ClientApp/node_modules` present

If frontend dependencies are missing:

```powershell
cd c:\Users\tscai\source\repos\Holonix\ClientApp
npm install
```

## Start Backend

```powershell
cd c:\Users\tscai\source\repos\Holonix\Server
dotnet run
```

Wait for the backend to report that it is listening on `http://localhost:5237` or `https://localhost:7241`.

## Start Frontend

```powershell
cd c:\Users\tscai\source\repos\Holonix\ClientApp
npm run start
```

Then open `http://localhost:4200`.

## Recommended Startup Order

1. Run `.\start-dev.ps1`.
2. Wait for the script to confirm both URLs are ready.
3. Open `http://localhost:4200`.

## Verification

- Backend health check: open `http://localhost:5237/swagger`
- Frontend check: open `http://localhost:4200`
- API calls from the frontend should use `/api/...` and flow through the Angular proxy

## Common Issues

- If the frontend fails because packages are missing, run `npm install` in `ClientApp`.
- If the backend fails to start, check local credentials for the connection string in `Server/appsettings.Development.json`.
- If ports are already in use, run `.\stop-dev.ps1` and then rerun `.\start-dev.ps1`.
- If you see backend startup delays, remember startup runs SQL seeders before the API begins listening.
- If backend startup fails, inspect `.dev-runtime/latest-logs.txt` to find the newest server/client logs before relaunching repeatedly.
