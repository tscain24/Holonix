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
- `Job`: business job summary with timing, cost, net cost, optional assigned `BusinessUser`, and optional recurrence.
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
- Business overview/workspace: loads business profile, services/sub-services, role-specific employee metadata, top employees, recent jobs, available role filters, and aggregates.
- Employees: paginated employee list with filters/sort; admins/owners can invite, close invites, deactivate employees, and update eligible roles.
- Services: owners can update selected parent services. Owners/admins can create, edit, assign, flag whether a customer service location is required, and soft-delete business sub-services.
- Availability: signed-in business member edits their own weekly availability for the business.
- Public business profile: exposes business identity, contact/location details, hours, parent services, and active sub-services without requiring login. Public-facing availability now prefers aggregated active-employee schedules over the legacy business-hours profile field: the hero status uses today's employee availability to show `Open` during active windows, `Closing soon` within 45 minutes of the end of the current window, and `Closed` otherwise, while the public schedule step loads date-specific availability for the selected day. Employee availability uses the native Sunday-first day index convention, while the legacy `businessHours` field remains a Monday-first frontend fallback. Backend availability calculations are now centralized in a shared calculator so the public booking feed and workspace availability views use the same "latest effective rows" rules, exclude all-day time off, and subtract partial-day employee time off windows from returned slots. The public cart's date-specific availability call is now service-aware: it sends the selected sub-service ids, the backend narrows the eligible employee pool to those service assignments, and each sub-service's `EmployeeCount` is treated as the simultaneous headcount required for that service before windows are intersected across the cart. The main content switches between Services, Business Hours, and Contact tabs, and `View Cart` navigates to a dedicated public cart screen while preserving the selected services for that business in session storage. Public scheduling is limited to dates from today through 45 days in advance. After date/time selection, the Details step summarizes services and schedule, and it loads the signed-in user's saved profile locations when any selected sub-service requires a customer service location.
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
