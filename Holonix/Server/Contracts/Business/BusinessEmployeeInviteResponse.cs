namespace Holonix.Server.Contracts.Business;

public sealed record BusinessEmployeeInviteResponse(
    long WorkloadId,
    string Email,
    string InvitedByDisplayName,
    DateTime InvitedAt,
    string Status);
