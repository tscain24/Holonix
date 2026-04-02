namespace Holonix.Server.Contracts.Business;

public sealed record BusinessEmployeeInviteResponse(
    long WorkloadId,
    string Email,
    string RoleName,
    string InvitedByDisplayName,
    string? InvitedByEmail,
    DateTime InvitedAt,
    string Status);
