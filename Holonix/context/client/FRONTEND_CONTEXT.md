# Holonix Frontend Context

Use this when a task touches Angular routing, shared services, auth session behavior, component layout conventions, or repeated UI workflows.

## App Shell

- Routing lives in `app-routing.module.ts`.
- Root declarations/imports live in `app.module.ts`.
- Feature components are under `features/`.
- API/session services are under `core/services/`.
- Global styles are in `src/styles.css`; most feature styling is colocated in each component CSS file.

## Core Services

- `auth.service.ts`: auth/profile API methods and DTO interfaces.
- `auth-session.service.ts`: local storage session state and refresh token flow.
- `auth.interceptor.ts`: bearer token attachment and refresh-on-401 for protected API calls.
- `business.service.ts`: business API methods and business DTO interfaces.
- `home.service.ts`: home-page API methods and home DTO interfaces.

## Common Component Patterns

- Feature pages check for `holonix_token` and redirect to `/login` if missing.
- Display name and avatar data come from local storage.
- `AuthSessionService.hasSessionExpiredFlag()` suppresses duplicate snack bars after forced logout.
- Most API errors are displayed with `err.error.errors[0]` fallback text.
- Image base64 values are normalized with helper methods that prepend `data:image/*;base64,` when needed.
- Business tab navigation should use `businessCode`.

## Styling Guidance

- Keep dense operational layouts; this app is not using marketing-page composition for authenticated workflows.
- Prefer feature-local CSS for page-specific layout.
- Avoid broad visual rewrites when fixing behavior.
- Angular Material snack bars use `snack-success` and `snack-error` panel classes.
- Existing business pages use table toolbars, row action popovers, modal state flags, and explicit loading/saving booleans.

## Build/Run

- From `ClientApp/`, build with `npm run build`.
- The dev command is `npm start`, but the repo-level `.\start-dev.ps1` is preferred because it coordinates backend and frontend ports.
