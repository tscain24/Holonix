namespace Holonix.Server.Contracts.Business;

public sealed record JobRequestAvailabilityResponse(
    DateOnly Date,
    int ExpectedEmployeeCount,
    IReadOnlyList<JobRequestServiceRequirementResponse> ServiceRequirements,
    JobRequestAvailabilityBookingResponse Request,
    IReadOnlyList<JobRequestAvailabilityBookingResponse> ExistingJobs);

public sealed record JobRequestServiceRequirementResponse(
    string ServiceName,
    int ExpectedEmployeeCount,
    IReadOnlyList<JobRequestServiceEligibleEmployeeResponse> EligibleEmployees);

public sealed record JobRequestServiceEligibleEmployeeResponse(
    long BusinessUserId,
    string DisplayName);

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
