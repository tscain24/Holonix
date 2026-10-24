# Frontend Business Context

Business frontend work spans this folder and `src/app/core/services/business.service.ts`.

## Screens

- `business-overview/`: user's business list and entry point. It also shows the signed-in member's current-week availability and assigned jobs across every associated business, with one row per business; clicking an assigned job opens its business, schedule, customer, and service-location details.
- Cross-business schedule jobs use their booking wall-clock date/time values when grouping and rendering, so a browser timezone conversion does not shift them into a different hour or day.
- `create-business/`: business creation wizard/form.
- `create-business/`: business creation wizard/form. Step 1 now uses a single location flow with no mode toggle: `Address 1` and `Address 2` are optional, `City` and `State` remain required, and `ZIP Code` is optional. The `Address 1` input shows street-address suggestions, while the `City` input shows city/state suggestions. When the user leaves `Address 1` blank, the form still requires a confirmed location before continuing, but it can satisfy that either from a picked city/state suggestion or by successfully resolving the typed city/state/ZIP on continue/launch. Step 2 no longer shows product-based vs service-based options; the form currently defaults hidden business-model state to service-based and does not surface that value in review.
- `business-workspace/`: overview tab, profile/general information editing, selected parent services, quick sub-service add/remove, job/employee summaries, delete/leave business. The hero action area includes a `View Public Page` button for active business members, which navigates to the customer-facing `/:businessCode` page using the current workspace `businessCode`. Pending job requests use a responsive decision-card layout that prioritizes service, schedule, customer service location, and payment authorization. Managers, administrators, and owners accept through a responsive assignment modal that repeats the service address, requested slot, existing jobs, conflict highlighting, and role-visible team working windows; it displays the expected team size as the sum of request-service employee counts, and `Assign & accept` stays disabled until that many employees are selected. Owners and administrators additionally receive `View cost breakdown`, opening a modal with immutable service lines, subtotal, the 5% customer fee, and the exact authorized total; older requests show aggregate totals with an itemization-unavailable note. Jobs created before service-location snapshots were introduced display `Location unavailable`. Workspace job timestamps are rendered as the booking wall-clock values carried by the API rather than applying the browser's timezone offset a second time.
- `business-employees/`: employees tab, invites, paged employees, filters, sorting, role update, deactivate.
- `business-service-manager/`: services tab focused on sub-services, assignment, filtering, paging, create/edit/delete.
- `business-availability/`: current user's weekly availability for the business.
- `public-business-page/`: public-facing business profile reached from search results or direct business-code URLs. The header status is computed from today's employee availability and shows `Open`, `Closing soon` in the last 45 minutes before the end of the current availability window, or `Closed`. The page still keeps the legacy `businessHours` profile, which uses a Monday-first `dayOfWeek` convention (`0 = Monday ... 6 = Sunday`), while employee availability follows the native Sunday-first convention (`0 = Sunday ... 6 = Saturday`). The public page uses employee availability as the source of bookable slots, and business hours act only as the outer time window that blocks/clips those slots when the business has at least one configured open-hours entry; an explicitly closed business-hours day blocks slots, but an unset/all-closed business-hours profile does not erase employee availability. Public booking surfaces should only consume public availability payloads and must not fall back to member/team endpoints that expose employee identities in browser-visible responses. Date-specific public slots now subtract both all-day and partial-day employee time off before the frontend clips them to business hours. On the cart scheduler, the date-specific availability call now sends the selected `businessSubServiceId` values so the backend can constrain slots to employees assigned to those services and apply each sub-service's required `employeeCount`; changing the cart refreshes the selected-date slot list. The left content area uses tabs for Services, Business Hours, and Contact, and the Business Hours tab reflects the configured business-hours profile rather than employee schedules. The top back action now preserves origin context per business code: search-origin visits keep returning to the last stored search results URL, while member-origin visits from the workspace use the caller-provided return URL and label the action `Back to business`; that same context is preserved when moving between the public page and `/:businessCode/cart`. Service selection is additive: users can add multiple sub-services to the cart and remove individual items from the booking summary. `View Cart` navigates to a dedicated public cart screen at `/:businessCode/cart`, selected service ids are persisted in session storage per business code so the cart survives that route change, and the cart route swaps the top back action to `Back to business`. During the `Service` step, the cart summary only shows service-level details and does not render placeholder date/time rows; its primary CTA is `View Cart` from the business page and `Schedule` once the user is already on the cart screen. Unauthenticated users may still build a cart, but once they reach the cart screen the primary CTA changes to `Log in to proceed` and redirects to `/login?returnUrl=...` instead of allowing scheduling until they sign in. Choosing `Schedule` after signing in advances the stepper into `Date & Time`, where the user selects a calendar day and then an available time slot generated from employee availability for that specific date, clipped to the business-hours window when configured. Date selection is limited to today through 45 days in advance; same-day slots earlier than or equal to the user's current local time are hidden, and visible calendar days are disabled once date-specific availability resolves with no usable slots for the selected services. The selected appointment display includes an end time computed from the start time plus the summed selected service durations. Continuing from a selected date/time opens the Details step, which reviews selected services and schedule; if any selected sub-service has `requiresServiceLocation`, it loads `AuthService.getProfile()`, auto-selects the user's primary saved location when available, falls back to the first saved location otherwise, and then re-initializes the embedded Stripe setup flow with that selected location. The page initializes Stripe only after the required booking details are present, calls the backend `setup-intent` route to fetch a client secret, mounts Stripe's Payment Element inline, orders the visible payment methods as card, US bank account, and Cash App Pay, and hides wallets such as Apple Pay, Google Pay, and Link. The page always confirms a `SetupIntent` on customer submit, never charges during authorization, and then calls a finalize route that creates the booking request in Holonix; redirect-return flows also finalize automatically from the `setup_intent` query param. After successful finalization, both flows navigate to `/home` and show the green success toast `Your Job Request was submitted successfully.` Failed authorization or finalization remains on checkout. If the business later accepts a request that starts within 24 hours, Holonix immediately charges the saved payment method during acceptance. On the workspace overview page, a new `Job Requests` section appears directly under the hero and shows those pending authorized bookings with accept/deny actions for managers, administrators, and owners.

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
- Business overview and workspace surfaces label the backend `totalJobs` aggregate as `Total Jobs Completed`; that value excludes every non-completed customer-booking status.
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
- The optional `Requires customer service location` checkbox is stored on the sub-service and exposed to public booking consumers.
- Effective date is required and cannot be in the past, except an unchanged existing edit date.
- Price must be zero or greater.
- Price can be `0` only when consultation needed is checked.
- Duration must be greater than zero and use 15-minute intervals.
- Employee count must be a positive whole number.
- Assigned users are active business employees.

The service manager table/cards now show whether each sub-service requires a customer service location, and the public business page renders that requirement as a booking pill/summary cue for selected services.

## Employees Table Rules

- Page size is 10 in `business-employees.component.ts`.
- Filters support `employee`, `role`, and `status`.
- `role` and `status` use `is` semantics and multi-value pipe-separated values.
- Sort fields are `employee`, `role`, `hiredDate`, `status`, and `assignedJobCount`.
- Inactive employees are only shown when the user toggles that mode and has invite/manage permissions.

## Job Request Staffing

- The assignment modal lists the expected employee count for every requested service and counts visible team members whose availability covers the complete request window. Expected staffing is advisory; acceptance requires at least one selected employee.
- Each requested service also lists its active assigned employees under `Can do this service`; this indicates qualification, while the timeline still indicates availability for the selected time.
- The accept result reports whether the job was accepted and charged or automatically cancelled because it was less than two hours from its scheduled start.

## Stripe Payment Loading

- The public cart keeps an animated loading state visible until Stripe reports that the embedded Payment Element is ready. After 10 seconds, the loader explains that startup is taking longer than usual; Stripe load failures replace it with an error message.
- A declined authorization keeps the current checkout details intact and offers `Try another card`, which returns the customer to the mounted Stripe Payment Element. Payment-form initialization failures offer `Try again` and reload the secure form.
- If acceptance-time charging fails, the workspace request immediately changes to `Payment Failed`, shows a failure toast, and keeps the card action labeled `Retry Payment`; successful requests retain `Assign & accept`.
- `Retry Payment` calls the charge-only payment-retry endpoint. If payment succeeds, the workspace opens the team-assignment modal; if it fails, the request stays on `Retry Payment`.
- Employee selections made before a failed charge are preserved when payment succeeds and the assignment modal reopens, so the previously selected team remains selected for final acceptance.
- `Decline` remains available for payment-failed requests so the business can close an unrecoverable request instead of retrying it.

- The dedicated `/workspace/jobs/:businessCode` Jobs tab is the dashboard surface with lifecycle status, upcoming/completed counts, completion history, employee reassignment controls, and a mark-complete action. Completion and reassignment refresh the workspace from the server so the display remains authoritative.
- Its visual structure is an operations dashboard: compact KPI cards, a searchable/filterable work-queue table, a responsive Today/Attention rail, and an anchored searchable people-picker for assignment changes.
- Opening the Jobs assignment picker loads daily team availability for that job date. Each employee is labeled Available, Busy, Day off, Outside working hours, or Unavailable; overlapping assigned jobs are treated as Busy, and only available employees can be selected.
- Product typography uses Geist Sans through the global `--holonix-font-sans` token, including Angular Material and business workspace screens.
- Workspace navigation uses the shared `app-workspace-breadcrumb`: Overview displays `Businesses / Workspace`; Employees, Services, Availability, and Jobs display `Workspace / [current page]`. The linked parent segment replaces duplicated Back controls.
- The primary workspace-section navigation is an understated underline tab strip beneath the breadcrumb; do not reintroduce pill-shaped navigation buttons beside it.
- The Availability screen follows the Jobs dashboard visual system: operational header, summary metrics, and compact white work surfaces on the shared neutral workspace background.
- Employees and Services use the same full-width dashboard canvas and direct breadcrumb-to-page-header flow as Jobs and Availability; their former secondary workspace tab rows are intentionally hidden.
- Jobs dashboard schedule labels preserve the booking's wall-clock date and time rather than applying the browser timezone conversion to the API timestamp. Use the Jobs component's wall-clock parser for schedule display, filtering, and availability conflict checks.
- Jobs assignment is team-based: the people picker supports selecting multiple available employees and saves the complete team together. The dashboard shows the returned `assignedEmployees` collection rather than only the legacy primary assignee.
- Workspace employee and job-assignment responses include profile-image base64 data. Jobs uses each member's photo in the assigned-team pill and picker, with initials as the fallback.
- The Business Overview "My Schedule" is a rolling seven-day view beginning today, rather than a Sunday–Saturday calendar week.
