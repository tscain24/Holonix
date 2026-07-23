# Server Business Context

Business backend work is concentrated in `Controllers/BusinessController.cs`, `Domain/Entities/BusinessDomain.cs`, `Infrastructure/Data/ApplicationDbContext.cs`, business contracts, and reference seeders.

## Main API Routes

Base route: `api/business`

- `GET /services`: active service catalog.
- `GET /countries`: country reference list.
- `GET /`: signed-in user's active business summaries.
- `GET /{businessCode}`: business workspace payload.
- `GET /public/{businessCode}`: public business profile payload, including aggregated weekly public availability derived from active employee schedules.
- `GET /public/{businessCode}/availability/daily`: public date-specific availability payload for booking surfaces. It accepts optional repeated `businessSubServiceIds` query values so booking availability can be constrained to the selected cart services.
- `POST /public/{businessCode}/checkout/setup-intent`: authenticated public-cart payment handoff that revalidates selected services, availability, and required saved location before creating a Stripe customer plus `SetupIntent` for later off-session charging.
- `POST /public/{businessCode}/checkout/complete`: authenticated finalize step that verifies a confirmed Stripe `SetupIntent`, creates a `Customer Booking` workload in `Awaiting Acceptance`, links it to a new `Job`, and stores the Stripe setup/payment method ids plus current payment state in `job.BookingPayment`.
- `GET /{businessCode}/job-requests/{workloadId}/availability`: manager/administrator/owner schedule-review route that returns the pending request plus accepted customer-booking and legacy jobs intersecting its date; the frontend combines it with the role-filtered team daily-availability route.
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
- `GET /{businessCode}/my-availability/daily`: current user's day-by-day availability projection for a date range.
- `PUT /{businessCode}/my-availability`: replace current user's weekly availability.
- `GET /{businessCode}/availability/team/daily`: role-filtered daily availability for visible team members.

## Entity Mapping

Important mappings in `Infrastructure/Data/ApplicationDbContext.cs`:

- `Business` -> `business.Business`; unique `BusinessCode`.
- `BusinessDetails` -> `business.BusinessDetails`; one-to-one with `Business`, restricted country FK, profile metadata only.
- `BusinessAddress` -> `business.BusinessAddress`; primary active address row is the source of truth for business address coordinates.
- `Service` -> `service.Category`.
- `BusinessService` -> `business.BusinessCategory`.
- `BusinessSubService` -> `business.BusinessService`.
- `BusinessSubServiceAssignment`, `BusinessRole`, `BusinessUser`, `BusinessUserRole`, `BusinessUserAvailability`, `BusinessUserTimeOff` -> `business` schema.
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
- Business-list and workspace `totalJobs` aggregates are derived through `JobWorkload` and count only `Customer Booking` workloads in `Completed` status, rather than all rows in `job.Job`.
- Normalize business code through the existing helper before querying.
- Use `GetActiveBusinessIdByCodeAsync` instead of repeating code/id fallback logic.
- Use `GetActiveBusinessRoleNameAsync` and `GetActiveBusinessUserIdAsync` when adding business-member checks.
- Keep role checks consistent with `CanManageEmployeeInvites` and `CanManageBusinessSubServices`.
- Use `BuildDisplayName` for user-facing names.
- Strip data URL prefixes before storing base64 image fields.
- Sub-service duration must be greater than zero and divisible by 15.
- `$0` sub-service pricing is only valid when consultation is needed.
- Sub-services carry a `RequiresServiceLocation` flag through create/update, workspace payloads, and public business profile payloads so later booking steps can decide whether to collect a customer location.
- Public business responses intentionally omit member-only workspace data and return only active parent services plus active sub-services.
- Shared availability calculation now lives in `Application/Interfaces/IBusinessAvailabilityCalculator.cs` and `Infrastructure/Services/BusinessAvailabilityCalculator.cs`. Use that service instead of duplicating "latest effective availability for date/day" logic inside controller actions.
- Public availability for customer-facing booking is aggregated from active `BusinessUserAvailability` rows for active business members. Employee availability keeps the backend/native Sunday-first `DayOfWeek` convention (`0 = Sunday ... 6 = Saturday`). Weekly public availability is merged per weekday in that native convention. The public daily availability endpoint, the team-daily availability endpoint, and the member daily-availability projection now all use the shared availability calculator so "latest effective rows for date/day" behavior stays aligned. That shared calculator excludes members on all-day time off and subtracts partial-day `BusinessUserTimeOff` windows from the returned time frames before merging. When the public daily endpoint receives selected `businessSubServiceIds`, it narrows the employee pool to the employees assigned to each requested sub-service, applies each sub-service's `EmployeeCount` as the required simultaneous headcount, and intersects those service-specific windows across the cart.
- The public Stripe payment flow currently uses `IStripeSetupIntentService` plus the official Stripe SDK to create Stripe customers and `SetupIntent` records with `usage=off_session`; the controller stores booking selection metadata in Stripe and returns the publishable key plus client secret so the frontend can mount Stripe's embedded Payment Element instead of redirecting to hosted Checkout. After client-side confirmation, a second route reads the confirmed SetupIntent back from Stripe and creates the authoritative job-request records in the Holonix database. When a manager, administrator, or owner accepts a booking that starts within 24 hours, the accept route creates an immediate off-session Stripe `PaymentIntent` from the saved customer/payment method and updates `BookingPayment` accordingly.
- For services requiring a customer location, checkout completion resolves the saved-location id from trusted SetupIntent metadata against the signed-in customer and copies its label, address, and coordinates onto the new `Job`. Job-request workspace and availability responses expose this immutable snapshot; older jobs without one return no service location.
- Checkout completion also creates one `JobServiceLine` snapshot per selected sub-service. Workspace responses expose line items, subtotal, 5% customer fee, and total only to owners and administrators; managers retain request-management permissions without receiving the cost breakdown.
- Public booking finalization writes scheduled job start/end values with UTC `DateTimeKind`, matching PostgreSQL `timestamp with time zone` requirements; the 24-hour acceptance-time charge check also compares against `DateTime.UtcNow`.
- Workspace overview now returns a `JobRequests` collection for `Customer Booking` workloads in `Awaiting Acceptance`, plus a `CanManageJobRequests` flag. Managers, administrators, and owners can update those requests through `POST /{businessCode}/job-requests/{workloadId}/accept` and `POST /{businessCode}/job-requests/{workloadId}/deny`.

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
