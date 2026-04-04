# Temporary Artifact Context

The `tmp-*` directories previously found under `Server/` were development verification artifacts, not application source.

Why they were a problem:
- `Server/Holonix.Server.csproj` uses `Microsoft.NET.Sdk.Web`, which implicitly treats many files under the project directory as content.
- Because these temporary folders lived inside `Server/`, they were being pulled into backend build output and startup staging.
- That inflated local build and restart times significantly.

Removed directories:
- `tmp-build-out`
- `tmp-business-500-fix-verify`
- `tmp-business-list-fix-verify`
- `tmp-business-overview-verify`
- `tmp-business-profile-edit-verify`
- `tmp-business-reportsto-final-verify`
- `tmp-business-role-inline-verify`
- `tmp-business-role-jobs-verify`
- `tmp-business-role-reporting-verify`
- `tmp-business-services-verify`
- `tmp-business-workspace-verify`
- `tmp-employee-card-verify`
- `tmp-general-info-verify`
- `tmp-launch-verify`
- `tmp-verify-build`
- `tmp-verify-build-delete`

Follow-up:
- `Server/Holonix.Server.csproj` now excludes `tmp-*` paths from project content so future temp folders do not pollute backend outputs.
