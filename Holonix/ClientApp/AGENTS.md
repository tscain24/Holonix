# ClientApp Agent Context

This is the Angular frontend for Holonix.

## Stack

- Angular CLI `16.2.16`.
- TypeScript `~5.1.3`.
- Angular Material/CDK `16.2.x`.
- RxJS `~7.8.0`.
- Tailwind and global CSS are present through `styles.css` and PostCSS.

## Commands

- Install dependencies only when needed: `npm install`
- Dev server: `npm start`
- Production build: `npm run build`
- Unit tests: `npm test`

The app expects the backend at `/api` via `proxy.conf.json`, targeting `http://localhost:5237`.
Local frontend development runs over `https://localhost:4200` so secure-origin Stripe features work during local testing. `npm start` expects the repo-level runtime cert files under `.dev-runtime/certs/`, which `.\start-dev.ps1` now exports from the current localhost dev certificate before launching Angular.

## App Structure

- `src/app/app-routing.module.ts`: route table.
- `src/app/core/services/`: API services, auth session, and HTTP auth interceptor.
- `src/app/features/auth/`: login and register screens.
- `src/app/features/home/`: authenticated home and business summary surface.
- `src/app/features/profile/`: user profile and recent activity.
- `src/app/features/business/`: create business, overview, workspace, employees, service manager, availability, and public business profile screens.
- `src/app/features/search/`: semantic service/business search results.
- Assets include Holonix logos and home-service imagery under `src/assets/`.
- Business-specific feature context: `context/client/business/BUSINESS_CONTEXT.md`.
- App-wide frontend context: `context/client/FRONTEND_CONTEXT.md`.

## Current Routes

- `/home`
- `/login`
- `/register`
- `/profile`
- `/terms`
- `/privacy`
- `/business`
- `/business/create`
- `/workspace/overview/:businessCode`
- `/workspace/employees/:businessCode`
- `/workspace/services/:businessCode`
- `/workspace/availability/:businessCode`
- `/search`
- `/:businessCode` for the public business page
- `/:businessCode/cart` for the public business cart screen

Legacy `/business/:businessCode*` routes still redirect to the workspace URLs.

## API Service Notes

- Auth API calls are in `core/services/auth.service.ts`.
- Business API calls and most business DTO interfaces are in `core/services/business.service.ts`.
- `business.service.ts` includes local-origin fallbacks for backend ports `5237`, `7241`, and `5000`; preserve this behavior unless explicitly removing it.
- Auth state is managed by `auth-session.service.ts`; token injection is handled by `auth.interceptor.ts`.

## UI Conventions

- Keep feature component HTML/CSS/TS colocated in each feature folder.
- Match the existing operational-tool style: dense, direct workflows instead of landing-page composition.
- Business pages use `businessCode` in navigation and API calls.
- Keep text and form validation user-facing messages aligned with backend `{ errors: string[] }` responses.
- Avoid broad visual rewrites when changing a specific workflow.
- Treat mobile responsiveness as part of completion for UI work; verify affected screens at mobile and desktop widths.

## Verification

- Run `npm run build` from `ClientApp/` for TypeScript/template checks.
- When checking live behavior, use the repo root `.\start-dev.ps1` so backend and frontend ports are coordinated and the Angular dev server is started over HTTPS.
- For UI changes, verify the affected page layout at mobile and desktop breakpoints before closing the task.
