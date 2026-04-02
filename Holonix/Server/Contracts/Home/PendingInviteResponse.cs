namespace Holonix.Server.Contracts.Home;

public sealed record PendingInviteResponse(
    long WorkloadId,
    string InviteType,
    DateTime InviteDate,
    string InviteCompany,
    string? InviteRole,
    string InvitedByDisplayName,
    string? InvitedByEmail,
    string Status);
