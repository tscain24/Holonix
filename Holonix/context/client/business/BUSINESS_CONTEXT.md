# Frontend Business Context

Business frontend work spans this folder and `src/app/core/services/business.service.ts`.

## Screens

- `business-overview/`: user's business list and entry point.
- `create-business/`: business creation wizard/form.
- `business-workspace/`: overview tab, profile/general information editing, selected parent services, quick sub-service add/remove, job/employee summaries, delete/leave business.
- `business-employees/`: employees tab, invites, paged employees, filters, sorting, role update, deactivate.
- `business-service-manager/`: services tab focused on sub-services, assignment, filtering, paging, create/edit/delete.
- `business-my-availability/`: current user's weekly availability for the business.

Routes are defined in `src/app/app-routing.module.ts` and use `businessCode`:

- `/business`
- `/business/create`
- `/business/:businessCode`
- `/business/:businessCode/employees`
- `/business/:businessCode/services`
- `/business/:businessCode/availability`

## Business API Client

`src/app/core/services/business.service.ts` owns:

- Business DTO interfaces used by business feature components.
- Calls to `/api/business`.
- Fallback direct backend origins: `http://localhost:5237`, `https://localhost:7241`, and `http://localhost:5000`.
- Employee filter serialization as `field~operator~encodedValue`, joined with semicolons.

Keep frontend interfaces aligned with `Server/Contracts/Business/*` when changing backend payloads.

## Shared UI/Data Patterns

- Components check `localStorage.getItem('holonix_token')`; missing token redirects to `/login`.
- Display name and profile image are read from local storage keys `holonix_display_name` and `holonix_profile_image_base64`.
- Session expiry is handled through `AuthSessionService.hasSessionExpiredFlag()`.
- API errors usually come as `{ errors: string[] }`; components show the first error in a Material snack bar.
- Image fields may be stored as raw base64 or data URLs; components normalize with `buildImageDataUrl`.
- Navigation between business tabs should keep using `businessWorkspace.businessCode`.

## Permission Rules In Components

- `business-workspace`: owner can manage parent services, edit profile/general information, delete business; owner/admin can manage sub-services; non-owner can leave.
- `business-employees`: owner/admin can invite, close eligible invites, update eligible roles, and deactivate eligible employees based on backend flags.
- `business-service-manager`: owner/admin can create/edit/delete sub-services.
- `business-my-availability`: any active business member can edit their own availability.

Backend remains the source of truth, but keep these guards aligned so UI does not expose impossible actions.

## Sub-Service Rules

Create/edit forms enforce:

- Parent service is required.
- Name is required.
- Effective date is required and cannot be in the past, except an unchanged existing edit date.
- Price must be zero or greater.
- Price can be `0` only when consultation needed is checked.
- Duration must be greater than zero and use 15-minute intervals.
- Employee count must be a positive whole number.
- Assigned users are active business employees.

## Employees Table Rules

- Page size is 10 in `business-employees.component.ts`.
- Filters support `employee`, `role`, and `status`.
- `role` and `status` use `is` semantics and multi-value pipe-separated values.
- Sort fields are `employee`, `role`, `hiredDate`, `status`, and `assignedJobCount`.
- Inactive employees are only shown when the user toggles that mode and has invite/manage permissions.
