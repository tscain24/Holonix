namespace Holonix.Server.Contracts.Business;

public sealed record CreatePublicBookingSetupIntentRequest(
    long[] BusinessSubServiceIds,
    DateOnly SelectedDate,
    string SelectedTime,
    long? UserSavedLocationId,
    bool AcceptedLegalPolicies);

public sealed record CreatePublicBookingSetupIntentResponse(
    string PaymentFlowType,
    string PublishableKey,
    string ClientSecret,
    string CustomerId,
    string? SetupIntentId,
    string? PaymentIntentId);
