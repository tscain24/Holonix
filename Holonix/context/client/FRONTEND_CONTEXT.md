# Holonix Frontend Context

Use this when a task touches Angular routing, shared services, auth session behavior, component layout conventions, or repeated UI workflows.

## App Shell

- Routing lives in `app-routing.module.ts`.
- Root declarations/imports live in `app.module.ts`.
- Feature components are under `features/`.
- API/session services are under `core/services/`.
- Global styles are in `src/styles.css`; most feature styling is colocated in each component CSS file.
- `app.component.html/css` own the shared shell wrapper, including route-aware spacing for the fixed global header and the global footer that renders Holonix brand links, support/legal contact links, social actions, and location/trust metadata across the app.

## Core Services

- `auth.service.ts`: auth/profile API methods and DTO interfaces.
- `auth-session.service.ts`: local storage session state and refresh token flow.
- `auth.interceptor.ts`: bearer token attachment and refresh-on-401 for protected API calls.
- `business.service.ts`: business API methods and business DTO interfaces.
- `home.service.ts`: home-page API methods and home DTO interfaces.
- `service-search.service.ts`: semantic search API methods for services/businesses.
- `public-business.service.ts`: public business profile API methods, including public daily availability lookups used by the booking flow.
- `mapbox-config.service.ts`: fetches the public Mapbox token for map/search UI.
- `search-origin.service.ts`: stores the user's current search origin in local storage.

## Common Component Patterns

- Feature pages check for `holonix_token` and redirect to `/login` if missing.
- Display name and avatar data come from local storage.
- `AuthSessionService.hasSessionExpiredFlag()` suppresses duplicate snack bars after forced logout.
- Most API errors are displayed with `err.error.errors[0]` fallback text.
- Image base64 values are normalized with helper methods that prepend `data:image/*;base64,` when needed.
- Business tab navigation should use `businessCode`.
- Search flow persists `holonix_search_origin_v1` in local storage and the last search URL in session storage.
- Public business browsing uses `/:businessCode`, and the public cart screen uses `/:businessCode/cart`.
- The public legal terms page is available at `/terms` and is linked from the shared footer.
- The public privacy policy page is available at `/privacy` and is linked from the shared footer.
- The public booking flow now loads employee-derived availability from the public business API instead of relying only on the static business-hours profile field. Signed-out users may add services to the public cart, but once they try to proceed from the cart they are sent to `/login` with a `returnUrl` back to the same public cart/business page; after successful login, the login screen honors that return URL. The Details step uses profile saved locations when selected services require a customer service location.
- The profile page includes a saved locations section backed by `auth.service.ts` CRUD calls to `/api/auth/profile/locations`; address entry uses Mapbox/geocoding autocomplete and validates typed addresses before saving so each saved location has coordinates for later booking/job flows.

## Styling Guidance

- Keep dense operational layouts; this app is not using marketing-page composition for authenticated workflows.
- Prefer feature-local CSS for page-specific layout.
- Avoid broad visual rewrites when fixing behavior.
- Responsive behavior is part of frontend correctness; check affected views at mobile and desktop widths, not just full-size desktop.
- Angular Material snack bars use `snack-success` and `snack-error` panel classes.
- Existing business pages use table toolbars, row action popovers, modal state flags, and explicit loading/saving booleans.

## Build/Run

- From `ClientApp/`, build with `npm run build`.
- Production build budgets currently warn at `1.5mb` initial bundle size and `22kb` per component style, with errors at `2mb` and `28kb`.
- The dev command is `npm start`, but the repo-level `.\start-dev.ps1` is preferred because it coordinates backend and frontend ports.
