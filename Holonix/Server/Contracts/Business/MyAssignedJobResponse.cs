namespace Holonix.Server.Contracts.Business;

public sealed record MyAssignedJobResponse(
    string BusinessCode,
    string BusinessName,
    long JobId,
    string JobName,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string CustomerName,
    BusinessWorkspaceJobLocationResponse? ServiceLocation);
