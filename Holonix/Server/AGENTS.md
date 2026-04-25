# Server Agent Context

This is the ASP.NET Core backend for Holonix.

## Stack

- Target framework: `net10.0`.
- Main entry point: `Program.cs`.
- Persistence: EF Core SQL Server with migrations in `Migrations/`.
- Auth: ASP.NET Core Identity plus JWT bearer tokens.
- API docs: Swagger is enabled in development.
- External services: Mapbox geocoding through `IGeocodingService` and `MapboxGeocodingService`.

## Important Files

- `Program.cs`: DI, Identity/JWT setup, CORS, migrations at startup, seeders, controller mapping.
- `Infrastructure/Data/ApplicationDbContext.cs`: EF model configuration and schema mapping.
- `Domain/Entities/BusinessDomain.cs`: most business, service, employee, job, workload, and reference entities.
- `Controllers/AuthController.cs`: registration/login/profile/token endpoints.
- `Controllers/BusinessController.cs`: business workspace, employee, invite, service, sub-service, availability, and business profile endpoints.
- `Application/Handlers/Auth/*`: register and login handler implementations.
- `Contracts/Auth/*` and `Contracts/Business/*`: request/response DTO records used by controllers and Angular.
- `context/server/BUSINESS_CONTEXT.md`: compact backend business route/domain map.
- `context/server/DATA_CONTEXT.md`: compact EF model, seeder, migration, and soft-delete guidance.

## Database Model Notes

- Default schema is `authentication` for Identity tables.
- Domain schemas include `business`, `service`, `job`, `work`, and `reference`.
- `Business.BusinessCode` is required, unique, and the preferred route identifier.
- `BusinessDetails` is a one-to-one row keyed by `BusinessId`.
- Most business-related relationships use `DeleteBehavior.Restrict`; do not assume cascade deletes except where configured.
- `ApplicationUser.NormalizedEmail` has a filtered unique index for active users: inactive users can free an email address.
- Active records are commonly filtered with `InactiveDate == null`, `IsActive`, and/or `EndDate == null`.

## Request Handling Patterns

- Use `User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")` for the current user id, matching existing controllers.
- Validation errors generally return `BadRequest(new { errors })`.
- Missing current-user context returns `Unauthorized(new { errors = new[] { "Invalid user context." } })`.
- Use `AsNoTracking()` for read-only EF queries.
- Use cancellation tokens on EF async calls.
- Prefer existing helper methods in `BusinessController` for business-code normalization, display names, role checks, and base64-image normalization before adding new copies.

## Seeders

Startup runs:

- `BusinessRoleSeeder.SeedAsync`
- `ServiceSeeder.SeedAsync`
- `WorkloadReferenceSeeder.SeedAsync`

When changing reference data behavior, check the related seeder before adding ad hoc data setup.

## Verification

- Build from repo root: `dotnet build .\Holonix.sln`
- Swagger after `.\start-dev.ps1`: `http://localhost:5237/swagger`
- Avoid creating temp directories under `Server/`; use repo-level `.dev-runtime` or an external temp location for verification artifacts.
