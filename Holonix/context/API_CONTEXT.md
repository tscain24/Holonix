# Holonix API Context

Use this when a task changes request/response shapes, API routes, frontend services, auth behavior, or error handling.

## API Surface

- Backend controllers live in `Server/Controllers/`.
- Backend DTO records live under `Server/Contracts/`.
- Angular API clients live under `ClientApp/src/app/core/services/`.
- Angular DTO interfaces are currently declared inside the service files, not in a separate generated client.
- Angular dev proxy maps `/api` to `http://localhost:5237`.

## Controllers

- `AuthController`: `/api/auth`
  - `POST /register`
  - `POST /login`
  - `POST /refresh`
  - `GET /profile`
  - `PUT /profile`
  - `POST /profile/locations`
  - `PUT /profile/locations/{userSavedLocationId}`
  - `DELETE /profile/locations/{userSavedLocationId}`
  - Profile location create/update requests must include a valid geocoded latitude/longitude pair; the Angular profile form resolves typed addresses through `/api/geocode/resolve-address` or Mapbox suggestions before saving.
  - `PUT /profile-image`
  - `DELETE /profile`
- `BusinessController`: `/api/business`; see `context/BUSINESS_CONTEXT.md` and `context/server/BUSINESS_CONTEXT.md`.
  - `GET /{businessCode}/payment-account` is Owner/Administrator-only and refreshes the stored business payment-account state from Stripe Accounts v2.
  - `POST /{businessCode}/payment-account/link` is Owner/Administrator-only. It idempotently creates or reuses the business Stripe recipient account and returns a short-lived Stripe-hosted Accounts v2 `account_onboarding` URL for initial or revisited setup. Redirect URLs come only from trusted backend configuration.
  - `POST /stripe/connect/webhook` verifies Stripe v2 thin-event signatures using `Stripe:ConnectWebhookSecret` and synchronizes known account, recipient-capability, and requirements changes by re-fetching current Stripe state.
  - Includes authenticated `GET /my-schedule/jobs?startDate=...&endDate=...`, which returns jobs assigned to the signed-in employee across active business memberships. It includes both multi-employee `JobAssignment` records and legacy primary `Job.BusinessUserId` assignments, plus customer and service-location details for the employee job-detail view.
  - Business summary and workspace `totalJobs` values count only jobs linked to `Customer Booking` workloads in `Completed` status.
  - Includes `GET /public/{businessCode}` for the public business profile.
  - Includes `GET /public/{businessCode}/availability/daily` for public date-specific booking availability. That route accepts optional repeated `businessSubServiceIds` query values to make public slots service-aware.
  - Includes authenticated `POST /public/{businessCode}/checkout/setup-intent` for the public cart's embedded Stripe authorization flow. The backend revalidates the selected public sub-services, chosen date/time, and required saved location, then creates a Stripe `SetupIntent` so Holonix can charge later after business acceptance.
  - Public booking SetupIntents use `usage=off_session` with Stripe automatic payment methods enabled. Stripe filters the Payment Element to methods compatible with future reuse; eligible Apple Pay and Google Pay wallets can appear on supported browsers/devices and registered payment-method domains, with card retained through the account's payment-method configuration.
  - Includes authenticated `POST /public/{businessCode}/checkout/complete` to finalize a confirmed Stripe `SetupIntent` into a persisted `Customer Booking` workload, linked `Job`, and `BookingPayment` row in `Awaiting Acceptance`.
  - Checkout completion resolves the authorized booking's saved-location metadata and snapshots the service address onto the job when any selected service requires a customer location; workspace job-request and availability responses expose that snapshot as `serviceLocation`.
  - Business workspace job requests include `costBreakdown` only for owners and administrators. New requests contain immutable per-service lines plus subtotal, the 5% customer service fee, and total; older requests retain aggregate values with an empty line list.
  - Includes authenticated `GET /{businessCode}/job-requests/{workloadId}/availability` for managers, administrators, and owners to compare a pending booking with accepted/legacy jobs on the same date before deciding it. It returns `serviceRequirements` with expected employee counts and active service-assigned employees per selected service, plus their summed `expectedEmployeeCount`.
  - `POST /{businessCode}/job-requests/{workloadId}/accept` requires `{ businessUserIds: number[] }`, validates active business membership, and makes the payment-timing decision against the current UTC clock while holding the business advisory lock. Starts more than 24 hours away are accepted with `Charge Pending` scheduled for exactly start minus 24 hours. Starts at or within 24 hours are charged immediately before acceptance completes. Passed starts return 409 without charging; there is no two-hour automatic cancellation rule.
  - `POST /{businessCode}/job-requests/{workloadId}/retry-payment` retries only the saved payment charge for a `Payment Failed` request. A successful retry leaves the request awaiting assignment so the workspace can open the assignment modal; a failed retry leaves it retryable.
  - Accepted-booking charges create and confirm one destination PaymentIntent using stable create/confirm idempotency keys derived from the workload. Before charging, the processor refreshes the Accounts v2 recipient and requires both `stripe_transfers` and payouts to be active. Unready businesses fail with an actionable payment-account message; Holonix never falls back to a platform-only charge.
  - The PaymentIntent amount remains `Job.Cost`; `transfer_data.destination` targets the stored connected business account and `application_fee_amount` is `Job.Cost - Job.NetCost`. Thus a $100 service with the existing 5% customer fee charges $105, routes $100 to the business, and retains $5 as Holonix's gross fee before Stripe processing costs. The Accounts v2 `fees_collector=application` configuration means Holonix bears those processing costs.
  - Ambiguous confirmation failures retrieve the known PaymentIntent before failure is reported. `BookingPayment` stores the connected account, PaymentIntent, Charge, Transfer, and Application Fee IDs. A succeeded PaymentIntent is not marked `Charge Succeeded` until distribution IDs reconcile.
  - `POST /api/stripe/payments/webhook` verifies the configured Stripe signing secret and idempotently reconciles matching `payment_intent.succeeded`, `payment_intent.payment_failed`, and `transfer.failed` events. Transfer failure is recorded separately as `Distribution Failed`; it is never described as a completed business payout. A hosted worker scans due accepted `Charge Pending` bookings each minute and uses the same charge processor and advisory lock as acceptance.
  - Includes `GET /{businessCode}/my-availability/daily` and `GET /{businessCode}/availability/team/daily`.
  - The daily availability routes now share a backend availability calculator so public and workspace surfaces stay aligned, including all-day time off exclusion and partial-day time off subtraction.
  - Business sub-service create/update and workspace/public profile payloads now include `requiresServiceLocation` so customer booking flows can tell when a selected service needs a user location on a later step.
  - `POST /api/business` still uses the same address fields, but the create-business screen now runs a single location flow: `address1` and `zipCode` may be blank, while `city` and `state` remain required. When the request is effectively a broader location instead of a street address, the frontend resolves and submits latitude/longitude before launch.
- `HomeController`: `/api/home`
  - `GET /pending-invites`
  - `POST /pending-invites/{workloadId}/accept`
  - `POST /pending-invites/{workloadId}/deny`
- `ServiceSearchController`: `/api/search`
  - `GET /services`
  - `GET /businesses`
- `GeocodingController`: `/api/geocode`
  - `GET /address-suggestions`
  - `GET /location-suggestions`
  - `POST /resolve-address`
    - Accepts either a street-address query or a broader city/state/ZIP query. When `address1` is blank, the resolver falls back to location-style geocoding instead of rejecting the request as incomplete.
- `MapboxController`: `/api/mapbox`
  - `GET /public-token`
- `DevCategoryEmbeddingsController`: `/api/dev/category-embeddings`
  - `POST /generate`
- `WeatherForecastController`: scaffold/sample endpoint; avoid using as a pattern for domain work.

## Contract Alignment

When changing a backend DTO, update the corresponding Angular interface and service method:

- `Server/Contracts/Auth/*` -> `ClientApp/src/app/core/services/auth.service.ts`
- `Server/Contracts/Business/*` -> `ClientApp/src/app/core/services/business.service.ts`
- `Server/Contracts/Home/*` -> `ClientApp/src/app/core/services/home.service.ts`

Keep property names camelCase on the frontend; ASP.NET Core JSON serialization maps C# PascalCase records to camelCase by default.

## Error Shape

Most domain validation/auth errors use:

```json
{ "errors": ["Message text."] }
```

Frontend components generally extract `err.error.errors` and show the first message in a snack bar. Preserve this shape for new validation failures unless there is a strong reason to introduce another error model.

## Authenticated Requests

- Protected Angular requests are `/api/**` and localhost absolute `/api/**` URLs.
- Login, register, and refresh are intentionally bypassed by the auth interceptor.
- Authenticated backend endpoints should use `[Authorize]`.
- Current user id is read from `ClaimTypes.NameIdentifier`, falling back to `"sub"`.

## Local-Origin Fallbacks

Some Angular services retry direct backend origins when proxy calls return status `0` or `404`. Existing fallback lists are intentionally local-dev oriented and include ports such as `5237`, `7241`, `5000`, `44367`, and `33823`.

Do not remove fallback behavior as incidental cleanup; it exists to tolerate backend/frontend launch differences during development.

- Business workspace job records expose lifecycle status, customer, completion metadata, and the full `assignedEmployees` team collection. `POST /api/business/{businessCode}/jobs/{jobId}/complete` records the authenticated assigned employee (or manager) and UTC completion timestamp; `POST /api/business/{businessCode}/jobs/{jobId}/reassign` validates active team membership before replacing assignments.
- Workspace employee records and `assignedEmployees` entries include optional `profileImageBase64` so operational surfaces can render the member's profile image.

## Financial Insights (phase 1)
- Authenticated GET /api/business/{businessCode}/insights and /insights/export require an active Owner or Administrator membership and role for that exact active business; membership/role start dates and inactive users are checked. Export reuses the same authorization and report query.
- Queries accept start/end (inclusive scheduled-start dates, maximum 367 days), groupBy (day/month/year/status/customer), status (Scheduled/Completed/Cancelled), employeeId, customer (optional exactCustomer), service snapshot name, value range, and bounded pagination (25 default, 100 maximum). At most 500 groups are returned; larger group sets require narrower filters. Export returns all aggregate groups, not just a job page.
- Reports use all matching jobs, not the workspace's eight-job preview. Pending requests/checkout attempts are excluded. Cancelled jobs are counted but excluded from value totals. Job completion requires CompletedDateTime; accepted workloads marked Completed do not imply completed work. Multiple assignments never multiply job totals.
- Payment status is informational only. BookingPayment has no captured/refunded amounts or balance ledger, so collected payments, outstanding balances, refunds and partial payment totals are not calculated. No payment workflows change.

- Insights responses now include employees: business membership ID (0 for Unassigned), name, participating jobs/completions/cancellations, equal-share booked and completed values. groupBy=employee returns these allocations as report groups with employeeId for exact drill-down. employeeId=0 selects unassigned jobs. Employee filtering selects whole jobs; allocation denominators always use their full teams. CSV includes labeled Report and Employee breakdown rows, or Employee rows only when grouped by employee. Both row types are separate views of the same value and must not be added together.

## Jobs operational API
- BusinessJobsController owns GET /api/business/{businessCode}/jobs (server-side search, status/date/employee filters, page 25 default/100 max, scoped aggregate KPIs and chronological active Today list capped at 100 with todayTotal), GET /jobs/options, GET /jobs/{id}, GET /jobs/{id}/team-options, POST /jobs, and POST /jobs/{id}/{ready|start|complete|reassign|reschedule|cancel}. Rescheduling an accepted but unpaid booking recalculates `ChargeScheduledForUtc`; already-succeeded payments are never charged again or rescheduled.
- All endpoints validate active/effective membership, role, user and business. Managers administer schedules/teams; assigned employees or managers can advance work. Owner/Administrator only: financial fields and manual job creation. OperationalJob carries revision plus explicit canReady/canStart/canComplete/canReassign/canReschedule/canCancel flags. Workspace TotalRevenue and job/request costs/net costs are now nullable and masked for other roles, closing the older API route around Insights permissions.
- Accepted bookings display as Scheduled. Only Scheduled -> Ready To Start -> In Progress -> Completed is allowed; start requires the scheduled time. Completion records actor/time. Pending checkout/acceptance/payment-failed records are excluded from operational queries and cannot be completed. Terminal jobs and in-progress assignments/schedules are locked. Empty teams are supported before starting. Reassign/reschedule returns work to Scheduled.
- Business-scoped PostgreSQL advisory transaction locks serialize Jobs schedule writes and booking decisions; revision mismatches return 409. Shared JobTeamValidator checks every selected active member against all overlapping accepted/manual/legacy jobs (including multi-assignee and legacy primary assignments), availability/time off for every date, service eligibility and staffing counts. Revalidation occurs on ready/start and booking acceptance as well as reassignment/rescheduling. No Stripe charge API calls or payment amounts changed.
- Paid cancellations are rejected with a payment-resolution explanation; no refund is implied or issued. New manual jobs use eligible existing fixed-price services and snapshots, create a Manual Job workload, and do not collect payment or fabricate a linked customer. Consultation jobs continue through customer booking. No schema migration is required; the reference seeder adds the manual workload type and lifecycle statuses.

- Manual-job options include eligible location-required services and existing business customer choices. Creation accepts an optional existing customer ID (validated against that business's prior jobs) and ServiceLocation with validated address/city/state/postal fields; required-location services cannot be created without a recorded address. No customer record, geocode or payment is fabricated. Active jobs sort ahead of terminal history in the paginated queue.

- Jobs GET accepts optional overlap=true for scheduling reads: with Start, includes jobs whose EndDateTime is after the start-day boundary; End still excludes jobs starting at/after the next day. Default list semantics, authorization and financial masking are unchanged. Angular JobsService.schedule uses this option and follows every page.
- Job location response builders return label=null, withholding private saved-location nicknames while preserving service address and coordinates. No storage or booking workflow change.
