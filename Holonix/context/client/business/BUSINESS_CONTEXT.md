# Frontend Business Context

Business frontend work spans this folder and `src/app/core/services/business.service.ts`.

## Screens

- `business-overview/`: user's business list and entry point.
- `create-business/`: business creation wizard/form.
- `business-workspace/`: overview tab, profile/general information editing, selected parent services, quick sub-service add/remove, job/employee summaries, delete/leave business.
- `business-employees/`: employees tab, invites, paged employees, filters, sorting, role update, deactivate.
- `business-service-manager/`: services tab focused on sub-services, assignment, filtering, paging, create/edit/delete.
- `business-availability/`: current user's weekly availability for the business.
- `public-business-page/`: public-facing business profile reached from search results or direct business-code URLs. The header status is computed from today's employee availability and shows `Open`, `Closing soon` in the last 45 minutes before the end of the current availability window, or `Closed`. The page still keeps the legacy `businessHours` profile, which uses a Monday-first `dayOfWeek` convention (`0 = Monday ... 6 = Sunday`), while employee availability follows the native Sunday-first convention (`0 = Sunday ... 6 = Saturday`). The public page uses employee availability as the source of bookable slots, and business hours act only as the outer time window that blocks/clips those slots when the business has at least one configured open-hours entry; an explicitly closed business-hours day blocks slots, but an unset/all-closed business-hours profile does not erase employee availability. Public booking surfaces should only consume public availability payloads and must not fall back to member/team endpoints that expose employee identities in browser-visible responses. Date-specific public slots now subtract both all-day and partial-day employee time off before the frontend clips them to business hours. On the cart scheduler, the date-specific availability call now sends the selected `businessSubServiceId` values so the backend can constrain slots to employees assigned to those services and apply each sub-service's required `employeeCount`; changing the cart refreshes the selected-date slot list. The left content area uses tabs for Services, Business Hours, and Contact, and the Business Hours tab reflects the configured business-hours profile rather than employee schedules. Service selection is additive: users can add multiple sub-services to the cart and remove individual items from the booking summary. `View Cart` navigates to a dedicated public cart screen at `/:businessCode/cart`, selected service ids are persisted in session storage per business code so the cart survives that route change, and the cart route swaps the top back action to `Back to business`. During the `Service` step, the cart summary only shows service-level details and does not render placeholder date/time rows; its primary CTA is `View Cart` from the business page and `Schedule` once the user is already on the cart screen. Choosing `Schedule` advances the stepper into `Date & Time`, where the user selects a calendar day and then an available time slot generated from employee availability for that specific date, clipped to the business-hours window when configured. Date selection is limited to today through 45 days in advance.

Routes are defined in `src/app/app-routing.module.ts` and use `businessCode`:

- `/business`
- `/business/create`
- `/workspace/overview/:businessCode`
- `/workspace/employees/:businessCode`
- `/workspace/services/:businessCode`
- `/workspace/availability/:businessCode`
- `/:businessCode` for the public business page
- `/:businessCode/cart` for the public business cart screen

Legacy `/business/:businessCode*` routes still redirect to the workspace URLs.

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
- Public business pages can be entered from search with navigation state, and they fall back to `sessionStorage['holonix_last_search_url_v1']` for the back-to-search action.
- Public business and search-related UI changes should be validated at mobile and desktop widths because these surfaces are customer-facing.

## Permission Rules In Components

- `business-workspace`: owner can manage parent services, edit profile/general information, delete business; owner/admin can manage sub-services; non-owner can leave.
- `business-employees`: owner/admin can invite, close eligible invites, update eligible roles, and deactivate eligible employees based on backend flags.
- `business-service-manager`: owner/admin can create/edit/delete sub-services.
- `business-availability`: any active business member can edit their own availability.

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
