# Server Business Context

Business backend work is concentrated in `Controllers/BusinessController.cs`, `Domain/Entities/BusinessDomain.cs`, `Infrastructure/Data/ApplicationDbContext.cs`, business contracts, and reference seeders.

## Main API Routes

Base route: `api/business`

- `GET /services`: active service catalog.
- `GET /countries`: country reference list.
- `GET /`: signed-in user's active business summaries.
- `GET /{businessCode}`: business workspace payload.
- `POST /`: create business and owner membership.
- `PUT /{businessCode}`: update business profile/general information.
- `DELETE /{businessCode}`: owner-only soft delete business.
- `PUT /{businessCode}/services`: update selected parent services.
- `POST /{businessCode}/services/{serviceId}/sub-services`: create business sub-service.
- `PUT /{businessCode}/sub-services/{businessSubServiceId}`: update sub-service and assignments.
- `DELETE /{businessCode}/sub-services/{businessSubServiceId}`: soft-delete sub-service.
- `GET /{businessCode}/employees`: paged employees with filters/sort.
- `POST /{businessCode}/employees/{businessUserId}/deactivate`: deactivate employee.
- `PUT /{businessCode}/employees/{businessUserId}/role`: change employee role.
- `GET /{businessCode}/employee-invites`: employee invite workloads.
- `POST /{businessCode}/employee-invites`: create invite workload.
- `POST /{businessCode}/employee-invites/{workloadId}/close`: close invite workload.
- `POST /{businessCode}/leave`: current user leaves business.
- `GET /{businessCode}/my-availability`: current user's availability for business.
- `PUT /{businessCode}/my-availability`: replace current user's weekly availability.

## Entity Mapping

Important mappings in `Infrastructure/Data/ApplicationDbContext.cs`:

- `Business` -> `business.Business`; unique `BusinessCode`.
- `BusinessDetails` -> `business.BusinessDetails`; one-to-one with `Business`, restricted country FK.
- `Service` -> `service.Service`.
- `BusinessService`, `BusinessSubService`, `BusinessSubServiceAssignment`, `BusinessRole`, `BusinessUser`, `BusinessUserRole`, `BusinessUserAvailability`, `BusinessUserTimeOff` -> `business` schema.
- `Job`, `JobWorkload`, `JobRecurrence`, `JobRecurrenceException` -> `job` schema.
- `Workload`, `WorkloadType`, `WorkloadStatus`, `EmployeeInviteWorkload` -> `work` schema.
- `Country` -> `reference.Country`.

Most relationships use `DeleteBehavior.Restrict`; code usually soft-deletes or end-dates rows instead of deleting graph data.

## DTO Locations

- Business create/update/profile/workspace/employee/service contracts: `Contracts/Business/*`.
- Auth token/profile response contracts: `Contracts/Auth/*`.
- Home invite surface: `Contracts/Home/PendingInviteResponse.cs`.

When changing an API shape, update the Angular interfaces in `ClientApp/src/app/core/services/business.service.ts`.

## Implementation Patterns

- Resolve current user id with `ClaimTypes.NameIdentifier` and fallback `"sub"`.
- Normalize business code through the existing helper before querying.
- Use `GetActiveBusinessIdByCodeAsync` instead of repeating code/id fallback logic.
- Use `GetActiveBusinessRoleNameAsync` and `GetActiveBusinessUserIdAsync` when adding business-member checks.
- Keep role checks consistent with `CanManageEmployeeInvites` and `CanManageBusinessSubServices`.
- Use `BuildDisplayName` for user-facing names.
- Strip data URL prefixes before storing base64 image fields.
- Sub-service duration must be greater than zero and divisible by 15.
- `$0` sub-service pricing is only valid when consultation is needed.

## Seeded Reference Data

Startup seeds:

- `BusinessRoleSeeder`: role names and hierarchy.
- `ServiceSeeder`: parent service catalog.
- `WorkloadReferenceSeeder`: workload types/statuses, including employee invite statuses.

Check seeders before hardcoding new role/status/service assumptions.

## Employee Filters

The employee list endpoint accepts:

- `pageNumber`
- `pageSize`
- `includeInactive`
- `filters`: semicolon-separated segments shaped like `field~operator~encodedValue`
- `sortBy`
- `sortDirection`

Supported filter fields are currently `employee`, `role`, and `status`. Supported operators are `contains` and `is`.
