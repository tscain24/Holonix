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

## App Structure

- `src/app/app-routing.module.ts`: route table.
- `src/app/core/services/`: API services, auth session, and HTTP auth interceptor.
- `src/app/features/auth/`: login and register screens.
- `src/app/features/home/`: authenticated home and business summary surface.
- `src/app/features/profile/`: user profile and recent activity.
- `src/app/features/business/`: create business, overview, workspace, employees, service manager, and availability screens.
- Assets include Holonix logos and home-service imagery under `src/assets/`.
- Business-specific feature context: `context/client/business/BUSINESS_CONTEXT.md`.
- App-wide frontend context: `context/client/FRONTEND_CONTEXT.md`.

## Current Routes

- `/home`
- `/login`
- `/register`
- `/profile`
- `/business`
- `/business/create`
- `/business/:businessCode`
- `/business/:businessCode/employees`
- `/business/:businessCode/services`
- `/business/:businessCode/availability`

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

## Verification

- Run `npm run build` from `ClientApp/` for TypeScript/template checks.
- When checking live behavior, use the repo root `.\start-dev.ps1` so backend and frontend ports are coordinated.
