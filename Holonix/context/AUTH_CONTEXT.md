# Holonix Auth And Session Context

Use this when a task touches login, registration, profile, JWTs, token refresh, local storage session data, or protected API calls.

## Backend

- `Server/Controllers/AuthController.cs` owns auth/profile endpoints.
- `Application/Handlers/Auth/RegisterUserHandler.cs` handles registration.
- `Application/Handlers/Auth/LoginUserHandler.cs` handles login.
- `Infrastructure/Services/TokenService.cs` creates JWTs.
- `Domain/Entities/ApplicationUser.cs` extends `IdentityUser` with `FirstName`, `LastName`, `DateOfBirth`, `ProfileImageBase64`, and `InactiveDate`.
- Identity uses EF Core stores in `ApplicationDbContext`.
- Development SQL auth installs `AzureCliSqlAuthenticationProvider` for `ActiveDirectoryDefault` when running on Windows.

## Frontend

- `ClientApp/src/app/core/services/auth.service.ts`: login/register/profile/profile-image/delete-profile calls and auth DTO interfaces.
- `ClientApp/src/app/core/services/auth-session.service.ts`: session persistence, refresh flow, expiry flag, token decoding, and raw refresh HTTP client.
- `ClientApp/src/app/core/services/auth.interceptor.ts`: attaches bearer tokens and retries once after refresh on protected 401 responses.

## Local Storage Keys

- `holonix_token`
- `holonix_display_name`
- `holonix_profile_image_base64`
- `holonix_session_expired`

Components commonly initialize header/user-menu state from these keys.

## Token Flow

- Login returns `userId`, `displayName`, `token`, and optional `profileImageBase64`.
- Business creation also returns a token and user display data because it may change session context.
- `AuthSessionService.getValidToken()` returns the stored token unless it expires within 120 seconds.
- Token refresh posts to `/api/auth/refresh` with the existing bearer token.
- Refresh uses `HttpBackend` to avoid passing through the interceptor.
- Concurrent refreshes share a single in-flight observable.
- If refresh fails, `logoutDueToExpiredSession()` clears session data, sets the expiry flag, and navigates to `/login`.

## Profile Rules

- Profile update requires first and last name.
- Date of birth must parse as `YYYY-MM-DD`.
- Profile image and business icon fields may arrive as data URLs; backend strips the `base64,` prefix before storing.
- User deletion is a soft delete: sets `InactiveDate`, locks out the user, changes username, and end-dates business memberships/roles.
- Active user queries should filter `InactiveDate == null`.

## Password Rules

Configured in `Program.cs`:

- Minimum length 8.
- Requires digit, uppercase, and lowercase.
- Does not require non-alphanumeric.
- Unique email is not enforced by Identity directly; active-email uniqueness is enforced by a filtered database index.
