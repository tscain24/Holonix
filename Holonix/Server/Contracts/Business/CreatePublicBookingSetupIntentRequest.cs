namespace Holonix.Server.Contracts.Business;

public sealed record CreatePublicBookingSetupIntentRequest(
    long[] BusinessSubServiceIds,
    DateOnly SelectedDate,
    string SelectedTime,
    long? UserSavedLocationId,
    bool AcceptedLegalPolicies);

public sealed record CreatePublicBookingSetupIntentResponse(
    string PublishableKey,
    string ClientSecret,
    string SetupIntentId,
    string CustomerId);
