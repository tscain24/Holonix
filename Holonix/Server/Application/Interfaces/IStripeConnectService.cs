namespace Holonix.Server.Application.Interfaces;

public interface IStripeConnectService
{
    Task<StripeConnectAccountResult> CreateAccountAsync(
        StripeConnectAccountRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<StripeConnectAccountResult> GetAccountAsync(
        string connectedAccountId,
        CancellationToken cancellationToken = default);

    Task<string> CreateAccountLinkAsync(
        string connectedAccountId,
        string returnUrl,
        string refreshUrl,
        bool updateExistingAccount,
        CancellationToken cancellationToken = default);

    string? ParseRelatedAccountId(string payload, string signatureHeader);
}

public sealed record StripeConnectAccountRequest(
    string BusinessCode,
    string BusinessName,
    string? BusinessEmail,
    string? BusinessPhoneNumber,
    string? CountryCode);

public sealed record StripeConnectAccountResult(
    string AccountId,
    string Status,
    string? TransfersCapabilityStatus,
    string? PayoutsCapabilityStatus);

public sealed class StripeConnectValidationException(string message) : Exception(message);
