# Server Business Context

Business backend work is concentrated in `Controllers/BusinessController.cs`, `Domain/Entities/BusinessDomain.cs`, `Infrastructure/Data/ApplicationDbContext.cs`, business contracts, and reference seeders.

## Stripe Connect Phase 1

- `BusinessPaymentAccountController` owns Connect operations. Every status/link request resolves the authenticated user's active membership and requires Owner or Administrator; Angular cannot supply Stripe account IDs or redirect URLs.
- `StripeConnectService` uses Stripe.NET Accounts v2 recipient configuration, requests `stripe_transfers`, provides Express dashboard access, and creates Stripe-hosted `account_onboarding` links for both initial and revisited information collection. A business-scoped Stripe idempotency key prevents repeated clicks from creating duplicate accounts.
- Account creation takes `identity.country` from the business's persisted `Country.TwoLetterIsoCode`, normalizes it to uppercase, and verifies Stripe Country Specs support before setting recipient configuration. Missing, malformed, or unsupported countries return a validation error without creating an account.
- The signed v2 thin-event endpoint `/api/business/stripe/connect/webhook` handles account, recipient capability, current-requirements, and future-requirements changes by retrieving current Stripe state. Payments-tab reads also refresh directly from Stripe.
- `Ready` requires recipient transfer and payout statuses to both be `active`. Requirement errors map to `action-required`, user-input requirements to `setup-incomplete`, and remaining non-active states to `verification-pending`.

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
- `GET /my-schedule/jobs`: authenticated current-user schedule endpoint. It returns assignments across the user's active business memberships for a supplied date range, honoring both `JobAssignment` and legacy `Job.BusinessUserId` records.

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
- SetupIntent creation explicitly enables Stripe automatic payment methods without supplying `payment_method_types`. This allows eligible Apple Pay and Google Pay card-wallet credentials to be collected for the unchanged future off-session charge while Stripe filters incompatible methods by flow, account, domain, browser, and device.
- For services requiring a customer location, checkout completion resolves the saved-location id from trusted SetupIntent metadata against the signed-in customer and copies its label, address, and coordinates onto the new `Job`. Job-request workspace and availability responses expose this immutable snapshot; older jobs without one return no service location.
- Checkout completion also creates one `JobServiceLine` snapshot per selected sub-service. Workspace responses expose line items, subtotal, 5% customer fee, and total only to owners and administrators; managers retain request-management permissions without receiving the cost breakdown.
- Public booking finalization writes scheduled job start/end values with UTC `DateTimeKind`, matching PostgreSQL `timestamp with time zone` requirements; the 24-hour acceptance-time charge check also compares against `DateTime.UtcNow`.
- Workspace overview returns a `JobRequests` collection for `Customer Booking` workloads in `Awaiting Acceptance` or `Payment Failed`, plus a `CanManageJobRequests` flag. Managers, administrators, and owners can deny new requests or accept/retry them with a required list of active business-user ids. Expected staffing is advisory and acceptance requires one or more active employees. Acceptance creates unique `JobAssignment` rows for every selected employee and also sets the first selection as the legacy `Job.BusinessUserId` primary assignee; payment timing follows the 24-hour rules below.
- Acceptance evaluates the service start while holding the existing business advisory lock. Starts beyond 24 hours become accepted `Charge Pending` bookings due at start minus 24 hours; starts at or within 24 hours charge immediately; passed starts are rejected without charging. There is no two-hour automatic cancellation. `ScheduledBookingPaymentWorker` scans due accepted payments each minute and calls the shared processor.
- The charge processor uses stable workload-derived idempotency keys for PaymentIntent create and confirm, retrieves the PaymentIntent after ambiguous confirmation errors, and never creates a second logical charge for retries or scheduler/acceptance races. Declines or customer-action-required results record `Charge Failed`; immediate failures also use the booking `Payment Failed` workload state.
- Before charging, `BookingPaymentProcessor` refreshes the business's Accounts v2 recipient and requires the existing `Ready` state: both `stripe_transfers` and payouts active. It then creates a destination PaymentIntent for `Job.Cost`, sends `Job.NetCost` to the connected account through `transfer_data.destination`, and retains `Job.Cost - Job.NetCost` through `application_fee_amount`. With `fees_collector=application`, Holonix bears Stripe processing fees; the configured business amount is unaffected.
- `BookingPayment` persists the destination account plus PaymentIntent, Charge, Transfer, and Application Fee IDs. Payment success remains `Distribution Pending` until those IDs reconcile. The signed `/api/stripe/payments/webhook` endpoint handles PaymentIntent success/failure and `transfer.failed`; duplicate or out-of-order payment failures cannot downgrade an already reconciled success, while a transfer failure becomes `Distribution Failed` and is not represented as a completed payout.
- Payment retry is charge-only; successful retry leaves the request awaiting assignment, and the normal accept action assigns the selected team after payment has succeeded.
- Payment-failed requests can still be denied by the business.

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

- Job completion is an audited state transition: an active assigned employee or manager may mark a job complete, which sets the linked customer-booking workload to `Completed` and stores `CompletedByUserId` plus `CompletedDateTime` on the job. Reassignment is manager/administrator/owner-only and validates active business memberships.

## Financial Insights
- BusinessInsightsController owns GET api/business/{businessCode}/insights and insights/export. Both enforce active Owner/Administrator membership and role, active business/user, and current start dates before any financial query. Unknown/unauthorized business codes return the same 403.
- Server-side aggregate queries operate on one row per job, using EXISTS filters for employee/service membership. Date range is bounded to 367 days, groups to 500 and job pages to 100 rows. Drill-down uses the same secured query and business-scoped assignments/service snapshots. CSV exports aggregate groups with formula-injection escaping.
- Legacy jobs and accepted/cancelled Customer Booking workloads are included. Cancellation excludes value; CompletedDateTime determines work completion independently of acceptance workload status. Amount basis is Job.Cost, not net cash. Missing payment amount/refund/balance/time-entry data prevents those analytics. No migrations or payment mutations.
- Server.Tests/BusinessInsightsTests.cs is a database integration test: isolated fixture writes in a rollback-only transaction using ApplicationDbContextFactory configuration; verifies role/business isolation, totals, cancellation and team deduplication.

- Insights EmployeeBreakdown aggregates shares server-side from distinct business-scoped JobAssignment teams with legacy primary-assignee fallback. Left-joined unassigned jobs retain their value in employee ID 0. Floor-to-cents plus deterministic remainder allocation makes employee amounts reconcile exactly; cancellations contribute counts but zero value. Membership IDs distinguish duplicate names; inactive memberships remain in historical assignment reporting. Employee groups are bounded to 500. CSV retains formula escaping and labels whole-job versus allocated data. Targeted integration coverage includes cent rounding, inactive assignees, legacy/team deduplication, unassigned filters, employee grouping, CSV, and permissions.

- BusinessJobsController and JobTeamValidator implement Jobs reads/mutations; see API_CONTEXT.md for contracts. Dedicated server-side paging/search/KPIs replace the eight-job dashboard dependency. Legacy jobs acquire a Manual Job workload when their first operational status changes; existing booking workloads retain their type.
- Reference seeding adds Manual Job (Scheduled, Ready To Start, In Progress, Completed, Cancelled). Existing and legacy assignments are keyed by membership ID; current inactive members remain visible in assignment options so they can be removed. Peer/higher-role availability is available to authorized job managers. Terminal assignment edits are rejected.
- Acceptance, the scheduled-payment processor, and Jobs rescheduling share the business advisory transaction lock. Rescheduling recalculates the due time only for unpaid `Charge Pending` bookings; succeeded charges are unchanged. Payment-failure records retain the existing commit behavior. Owners/Admins receive financial values; other roles receive null in both Jobs and legacy workspace job/request/aggregate fields.
- Server.Tests/BusinessJobsTests.cs uses isolated fixtures in a rollback-only database transaction to verify pagination, team search, permissions, lifecycle transitions, pending/cancelled/completed guards, stale updates, conflicts, qualifiers, overnight coverage, creation/rescheduling, clearing/inactive teams, paid cancellation restrictions and legacy workspace masking.

- BusinessJobsController List supports optional JobsQuery.Overlap for calendar reads, using EndDateTime > Start midnight while retaining the existing exclusive End + 1 day start boundary. Normal Jobs list date filtering stays unchanged; carry-over jobs are covered by BusinessJobsTests. No scheduling writes or authorization rules changed.
- Operational jobs and the shared BusinessController job-location response builder suppress saved-location labels at serialization (null). Personal saved-location storage remains unchanged.
