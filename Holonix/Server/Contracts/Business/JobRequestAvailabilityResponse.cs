namespace Holonix.Server.Contracts.Business;

public sealed record JobRequestAvailabilityResponse(
    DateOnly Date,
    JobRequestAvailabilityBookingResponse Request,
    IReadOnlyList<JobRequestAvailabilityBookingResponse> ExistingJobs);

public sealed record JobRequestAvailabilityBookingResponse(
    long JobId,
    long? WorkloadId,
    string Name,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string Status,
    string? AssignedEmployeeName,
    bool IsPendingRequest,
    BusinessWorkspaceJobLocationResponse? ServiceLocation);
