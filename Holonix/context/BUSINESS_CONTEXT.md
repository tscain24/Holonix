# Holonix Business Domain Context

Use this when a task touches businesses, workspaces, employees, services, sub-services, jobs, or workloads.

## Product Model

Holonix models service businesses. A signed-in user can create a business, become its owner, configure offered services/sub-services, manage employees/invites, track job summaries, and set their own availability. The app also exposes a public business profile and a search flow for discovering businesses by category/service near a chosen location.

The business workspace is centered on `Business.BusinessCode`, a generated 10-character public identifier. Prefer `businessCode` in routes, links, API calls, and UI state.

## Core Concepts

- `Business`: top-level company row with `BusinessCode`, `Name`, `StartDate`, `InactiveDate`, `IsProductBased`, and `IsRecurring`.
- `BusinessDetails`: one-to-one business profile/address/contact/icon/percentage row.
- `Service`: reference catalog service seeded at startup.
- `BusinessService`: selected parent services for a business.
- `BusinessSubService`: business-specific offerings under a parent service, with price, duration, employee count, effective date, consultation flag, a `RequiresServiceLocation` booking hint, and soft-delete `InactiveDate`.
- `BusinessSubServiceAssignment`: active employees assigned to a sub-service.
- `BusinessUser`: membership between an app user and a business, with activity dates and optional reporting manager.
- `BusinessUserRole`: dated role assignment for a business member.
- `BusinessRole`: seeded roles with hierarchy numbers.
- `BusinessUserAvailability` and `BusinessUserTimeOff`: employee schedule data.
- `Job`: business job summary with timing, cost, net cost, optional legacy primary `BusinessUser`, optional recurrence, an immutable customer service-location snapshot for location-required bookings, and immutable `JobServiceLine` pricing snapshots for customer-booking cost breakdowns.
- `JobAssignment`: many-to-many assignment from an accepted job to one or more active business employees.
- `Workload`, `EmployeeInvite`, `EmployeeInviteWorkload`: invite workflow records and statuses.

## Roles And Permissions

Seeded role names are `Owner`, `Administrator`, `Manager`, and `Employee`.

- Owner: can manage services, profile/general information, employees, sub-services, delete business, and invite eligible roles.
- Administrator: can manage employee invites and sub-services, but frontend currently does not allow full service/profile ownership actions.
- Manager/Employee: narrower business access; non-owner users may leave a business.
- Non-owner roles require `ReportsToUserId` according to `BusinessRoleNames.RequiresReportsToUserId`.

Backend permission checks live mostly in `Server/Controllers/BusinessController.cs`. Frontend permission guards are duplicated in business feature components, so keep both sides aligned.

## Important Workflows

- Create business: creates `Business`, `BusinessDetails`, selected `BusinessService` rows, active `BusinessUser`, owner `BusinessUserRole`, and returns a refreshed auth token. The create-business wizard now uses one shared location flow instead of separate full-address vs general-location modes. `Address 1` and `Address 2` are optional, `City` and `State` remain required, and the submit flow resolves coordinates either from the typed street address or, when the street fields are blank, from the entered city/state/ZIP location. The business-model picker is currently hidden in the UI and new businesses default to service-based while the backend payload still includes `IsProductBased = false`.
- Business overview/workspace: loads business profile, services/sub-services, role-specific employee metadata, top employees, recent jobs, available role filters, and aggregates. The `Total Jobs Completed` aggregate counts only customer-booking workloads whose status is `Completed`. Pending customer bookings open a team-assignment modal that compares the requested time with existing jobs and role-visible team working windows, displays expected staffing as a recommendation, and requires at least one employee to be selected. Acceptance charges immediately when service starts within 24 hours, schedules payment for exactly 24 hours before service when farther away, and rejects passed starts without charging. Last-minute future requests are allowed; there is no two-hour cancellation cutoff.
- Personal cross-business schedule: the business overview requests the signed-in member's assigned jobs for the current week and renders them alongside availability for every active business membership. Both multi-employee assignments and legacy primary assignments appear.
- Employees: paginated employee list with filters/sort; admins/owners can invite, close invites, deactivate employees, and update eligible roles.
- Services: owners can update selected parent services. Owners/admins can create, edit, assign, flag whether a customer service location is required, and soft-delete business sub-services.
- Availability: signed-in business member edits their own weekly availability for the business.
- Public booking uses an authenticated embedded Stripe SetupIntent flow: submission never charges, and only saves the reusable payment method plus booking metadata. Confirmed setup creates an `Awaiting Acceptance` booking. Future same-day times remain requestable until their scheduled start; both setup and finalization reject already-passed starts. Business acceptance then either charges immediately at or within 24 hours or records a scheduled charge for exactly 24 hours before service.
- Accepted-booking charges use Stripe Connect destination charges only after the business's live Accounts v2 recipient transfer and payout capabilities are active. The customer total remains service subtotal plus the existing rounded 5% fee; Stripe routes the subtotal to the connected business and returns the fee to Holonix as an application fee. Holonix is the configured fee/loss collector and therefore bears Stripe processing costs and platform negative-balance responsibility. This records a connected-account transfer, not completion of the business's later bank payout.
- Search: uses location-aware service/category search to find matching businesses and route into the public business page.
- Delete business: owner-only soft delete that also ends active business memberships and roles.
- Leave business: non-owner member exits the business.

## Data Rules

- Active businesses have `InactiveDate == null`.
- Active users have `InactiveDate == null`.
- Active business members generally require `IsActive == true` and `EndDate == null`.
- Current roles generally require `BusinessUserRole.EndDate == null`.
- Active sub-services have `InactiveDate == null`.
- Backend validation commonly returns `{ errors: string[] }`; frontend snack bars typically display the first error.

## Context Files

- Backend business details: `context/server/BUSINESS_CONTEXT.md`
- Frontend business details: `context/client/business/BUSINESS_CONTEXT.md`

- Overview prioritizes operational KPIs, requests/payment attention, unassigned work, and the next five scheduled jobs; expandable resources retain profile/contact, services, team, and full job controls.

- Workspace navigation and service page headings use the user-facing label Services.

- The Services page title is simply Services, avoiding repeated words in business names.

- Jobs and Employees use standalone page titles consistent with Services.

## Payments and Financial Insights — first phase
- Owners and Administrators can open Payments from the workspace navigation. The Payment Account surface uses Stripe-hosted Accounts v2 onboarding/update links and displays Stripe-derived state; all existing financial reporting remains below it. `Ready` requires both recipient `stripe_transfers` and payouts to be active. The reporting page supports scheduled-date filters, server-side KPIs/grouped reports, selectable metrics, table/bar views, drill-down into paginated underlying jobs, and CSV aggregate export.
- Connect Phase 1 creates recipient accounts with Express dashboard access and application-collected fees/loss responsibility, using a Stripe idempotency key scoped to the Holonix business. Holonix stores only the connected account identifier and synchronization state; bank and verification details remain with Stripe. No transfers, platform fees, customer checkout, or charge-timing behavior is implemented or changed in this phase.
- Booked job value means accepted/legacy Job.Cost (including the customer fee), excluding cancellations; it is not revenue collected. Completed value uses the completion timestamp. Upcoming value uses future scheduled starts against UTC because a business timezone is not stored. Employee/team attribution, service revenue allocation, cash/refund/balance metrics and actual worked time are not invented.
- Later phases: saved reports/templates, persistent personalized dashboards, widget reorder/resize, more charts/groupings and advanced filters. Current metric selection lasts for the page visit.

- Financial Insights includes a 12-month job-value trend ending on the selected report end date, respecting all non-start-date report filters. It excludes cancellations, includes empty months, and labels partial-month comparisons. Selecting a month drills into its jobs.

- Employee breakdown is available on Financial Insights and in every CSV export. Each job value is split equally across distinct assigned employees, with any remaining cents allocated by ascending business membership ID; legacy primary assignee applies only when no team exists. Unassigned value has its own row. Current/former employee assignments remain reportable. Participation counts overlap across employees; dashboard totals still count jobs once. Clicking an employee filters matching whole jobs, while breakdown rows show allocated shares.

- Financial Insights employee selection is a top-row filter rather than a separate dashboard section. Employee grouping and CSV breakdowns are retained.

- Payments is a workspace navigation tab beside Jobs, with a matching mobile-menu entry. Visibility uses the existing backend Owner/Administrator financial permission. The former Overview hero shortcut remains removed. Insights API authorization still protects direct URLs and exports.

## Jobs operational workflow
- Jobs now uses a dedicated paginated operational API, not the Overview preview. Counts/search cover all matching work. Pending requests remain in Overview; cancelled work is excluded from active KPIs and booked value. Full-team labels/search, chronological active Today work and explicit overdue/unassigned filters are supported.
- Scheduled -> Ready To Start -> In Progress -> Completed is explicit and permission-controlled. Managers own scheduling and staffing; assigned employees can advance work. Completion metadata is shown, terminal team/schedule changes are blocked, and stale mutations are rejected. Historical employee allocations cannot be changed through completed-job reassignment.
- New job, Filters, Details, date-range schedule view, team editing/unassignment and rescheduling are functional. Manual creation is Owner/Admin-only, uses fixed-price services and requires a recorded address for location-required services, records value without collecting payment, and can link an existing business customer (otherwise the customer remains explicitly unlinked). Charged bookings cannot be cancelled through Jobs without a payment-resolution workflow.

- Jobs rescheduling uses an inline Material calendar, date shortcuts, separate start/end time and end-day selectors, a duration summary and current/new schedule comparison. Existing server staffing/availability and lifecycle checks remain authoritative.

### Availability scheduling workspace (October 2026)
- Availability uses calendar-first month/week views and a role-scoped weekly Team Schedule. Date details show business opening hours, personal hours, visible team availability, active jobs, and potential conflicts. Team daily reads retain existing hierarchy authorization; unavailable data has explicit errors rather than zero capacity.
- Owner-only business opening hours editing reuses the existing full-profile update endpoint, refreshing the workspace before composing the payload to preserve other profile fields. Personal hours retain separate focused editing, multiple windows/breaks, validation, and existing effective-date persistence. Missing personal schedules no longer display fabricated weekday availability.
- Personal time off uses a focused dialog with full/partial single-day or full multi-day absence, existing entry editing/removal and locked-date rules. Cancel restores staged entries. New date ranges query overlapping active jobs and warn without changing assignments. Employee or business-wide time-off creation remains unsupported by this API.
- Calendar jobs are fully paginated using the optional Jobs `overlap=true` query. Start dates then match jobs ending after the start-day boundary; end dates retain the exclusive next-day boundary. Existing Jobs filters keep their original start-date behavior by default.
- Unbooked team windows subtract active assigned jobs from server-calculated daily working windows. Potential conflicts flag overlapping assignments or work outside those windows. These are not guaranteed bookable capacity: travel, qualifications, business hours and booking rules still apply. No utilization metrics, new fields, or automatic schedule changes.
- Availability calendar uses an explicit seven-column CSS grid with stacked date-cell content at desktop and mobile breakpoints; no scheduling data or workflow change.
- Availability scheduled-work and Team Schedule job buttons open a read-only preview modal using the existing authorized operational job data. The modal shows schedule, status, customer/contact, team, services, location, and job value/payment only when cost is present; Go to Jobs opens the existing business Jobs tab. No job updates or new API calls.
- Job service locations in Availability previews and Jobs details link to Google Maps in a new tab, using only the encoded postal address. Saved-location nicknames are excluded from display and from operational/workspace/assigned-job location DTOs; stored saved-location names remain intact for personal location management.
- The Your Businesses "My Schedule" job modal also links its address-only service location to Google Maps in a new tab. Missing locations remain plain "Location unavailable" text.
- Marketplace search now emphasizes clickable business cards and the existing interactive map. It does not expose fabricated ratings, photos, prices or verification; the current search DTO supports a business logo, category, distance and coordinates only. Search ranking and public business routes are unchanged.
- The public business profile presents real business identity, location, operating status, service catalog data, and booking-cart totals in a responsive marketplace layout. Ratings and reviews remain omitted until supported by the public profile API. The existing public route, session-backed service selection, availability flow, saved-location requirements, authentication return path, and Stripe authorization workflow are unchanged.
- Empty public service categories are hidden, while selected services retain an itemized desktop summary and existing cart/scheduling behavior. The summary's sticky offset respects the fixed application header.
- The public cart presents non-destructive service removal, conditionally displays consultation requirements, and separates subtotal, existing service fees, and total without changing any price, duration, availability, persistence, or payment calculations.
- Selected public services now proceed directly from the business profile to Date & Time, including across the existing login return path. Customers return to the public profile to change services. Scheduling presentation is wider and calendar-focused on desktop while retaining the existing availability, duration, pricing, details, and Stripe workflows.
- Scheduling balances the desktop calendar and available-time panels at roughly 60/40 without forcing equal heights; the existing selected appointment summary reports date, start time, and total selected-service duration.
- Booking Details uses one compact service-and-schedule summary, retains the separate saved-location selection, and clearly explains that request submission saves an eligible payment method without charging it. Customer consent authorizes the displayed total for a later off-session charge if the business accepts; Stripe dynamically limits the Payment Element to methods eligible for the existing off-session SetupIntent.
- Details and Payment visual polish reduces nested card borders, compacts summary/location spacing, and emphasizes the final total and booking action; all selection, availability, authorization, and payment behavior remains unchanged.
- Your Businesses distinguishes backend transport/load failures from an authoritative empty business list and provides an in-place retry; no business membership or profile data is changed.
