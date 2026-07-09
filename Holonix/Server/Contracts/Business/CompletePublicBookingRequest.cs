namespace Holonix.Server.Contracts.Business;

public sealed record CompletePublicBookingRequest(
    string? SetupIntentId,
    string? PaymentIntentId);

public sealed record CompletePublicBookingResponse(
    long JobId,
    long WorkloadId,
    string Status,
    string PaymentStatus);
