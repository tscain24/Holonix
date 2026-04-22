# Server Data And Migration Context

Use this when a task changes EF entities, model configuration, migrations, seeders, or database behavior.

## DbContext

`Infrastructure/Data/ApplicationDbContext.cs` is the authoritative EF model map.

- Default schema is `authentication`.
- Identity tables are renamed to `Users`, `Roles`, `UserRoles`, `UserClaims`, `UserLogins`, `RoleClaims`, and `UserTokens`.
- Domain schemas include `business`, `service`, `job`, `work`, and `reference`.
- Most domain relationships use `DeleteBehavior.Restrict`.
- Startup runs `Database.MigrateAsync()` in `Program.cs`.

## Active/Soft Delete Patterns

- Users: `ApplicationUser.InactiveDate == null`.
- Businesses: `Business.InactiveDate == null`.
- Business users: `IsActive == true` and `EndDate == null`.
- Business user roles: `EndDate == null`.
- Sub-services: `InactiveDate == null`.
- Deactivation usually updates timestamps/status fields rather than deleting rows.

## Important Indexes And Constraints

- Active user email uniqueness: filtered unique `EmailIndex` on `NormalizedEmail` where `NormalizedEmail IS NOT NULL AND InactiveDate IS NULL`.
- `Business.BusinessCode` is unique.
- `Country.Name`, `TwoLetterIsoCode`, and optional `ThreeLetterIsoCode` are unique.
- Active `BusinessSubService` names are unique per `(BusinessId, ServiceId, Name)` with an `InactiveDate IS NULL` filter.
- `BusinessSubServiceAssignment` is unique per `(BusinessSubServiceId, BusinessUserId)`.
- `EmployeeInviteWorkload` has unique indexes on both `WorkloadId` and `EmployeeInviteId`.

## Seeders

Startup seeders in `Program.cs`:

- `BusinessRoleSeeder`: `Owner` 1000, `Administrator` 800, `Manager` 700, `Employee` 500.
- `ServiceSeeder`: canonical title-cased service catalog; removes deprecated unassigned services.
- `WorkloadReferenceSeeder`: `Employee Invite` workload type with `Pending Invite`, `Active`, `Denied`, and `Closed` statuses.

Add or change reference behavior in seeders rather than scattering setup logic through controllers.

## Migration Guidance

- Existing migrations are historical source. Avoid editing old migrations unless the user explicitly asks.
- For model changes, create a new migration and update `ApplicationDbContextModelSnapshot`.
- Designer files and snapshot files are large; avoid opening them unless diagnosing EF model history.
- `Server/Holonix.Server.csproj` excludes `tmp-*` paths because the Web SDK otherwise treats many project files as content.
- Keep verification artifacts outside `Server/` when possible.

## Date/Time Types

- `DateOnly` is used for business start dates, birthdays, effective dates, recurrence dates, and availability effective dates.
- `TimeOnly` is used for availability and recurrence times.
- `DateTime.UtcNow` is commonly used for created/start/end/deactivation timestamps.
