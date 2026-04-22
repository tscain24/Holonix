# Holonix Business Domain Context

Use this when a task touches businesses, workspaces, employees, services, sub-services, jobs, or workloads.

## Product Model

Holonix models service businesses. A signed-in user can create a business, become its owner, configure offered services/sub-services, manage employees/invites, track job summaries, and set their own availability.

The business workspace is centered on `Business.BusinessCode`, a generated 10-character public identifier. Prefer `businessCode` in routes, links, API calls, and UI state.

## Core Concepts

- `Business`: top-level company row with `BusinessCode`, `Name`, `StartDate`, `InactiveDate`, `IsProductBased`, and `IsRecurring`.
- `BusinessDetails`: one-to-one business profile/address/contact/icon/percentage row.
- `Service`: reference catalog service seeded at startup.
- `BusinessService`: selected parent services for a business.
- `BusinessSubService`: business-specific offerings under a parent service, with price, duration, employee count, effective date, consultation flag, and soft-delete `InactiveDate`.
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

- Create business: creates `Business`, `BusinessDetails`, selected `BusinessService` rows, active `BusinessUser`, owner `BusinessUserRole`, and returns a refreshed auth token.
- Business overview/workspace: loads business profile, services/sub-services, role-specific employee metadata, top employees, recent jobs, available role filters, and aggregates.
- Employees: paginated employee list with filters/sort; admins/owners can invite, close invites, deactivate employees, and update eligible roles.
- Services: owners can update selected parent services. Owners/admins can create, edit, assign, and soft-delete business sub-services.
- Availability: signed-in business member edits their own weekly availability for the business.
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
