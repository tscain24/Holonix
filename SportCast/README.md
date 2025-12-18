# SportCast

Minimal ASP.NET Core Web API + Angular (ClientApp) starter scaffold.

## Structure
- Server: `Server` — ASP.NET Core Web API (net8.0)
- Client: `ClientApp` — Angular app (created with @angular/cli v16)

## Requirements
- .NET 8+ SDK
- Node.js 20.x (upgrade to >=20.19 for latest Angular CLI)
- npm

## Run (development)
Run backend:

```powershell
cd Server
dotnet run
```

Run frontend (dev server):

```powershell
cd ClientApp
npm install
npx ng serve --open
```

## Build (production)
Build backend solution:

```powershell
cd c:\Users\tscai\source\repos\SportCast
dotnet build .\SportCast.sln
```

Build frontend production bundle:

```powershell
cd ClientApp
npx ng build --configuration=production
```

## Notes
- The `dotnet new angular` template was not available with the installed .NET templates, so this project uses a standalone Angular app created with the Angular CLI.
- If you want the latest Angular CLI features, upgrade Node.js to >=20.19 or install a compatible Node version manager.
