namespace Holonix.Server.Application.Interfaces;

public interface IStripeSetupIntentService
{
    Task<StripeSetupIntentResult> CreateSetupIntentAsync(
        StripeSetupIntentRequest request,
        CancellationToken cancellationToken = default);

    Task<StripePaymentIntentResult> CreatePaymentIntentAsync(
        StripePaymentIntentRequest request,
        CancellationToken cancellationToken = default);

    Task<StripeRetrievedSetupIntentResult> GetSetupIntentAsync(
        string setupIntentId,
        CancellationToken cancellationToken = default);

    Task<StripeRetrievedPaymentIntentResult> GetPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);

    Task<StripeRetrievedPaymentIntentResult> ChargeSavedPaymentMethodAsync(
        StripeOffSessionChargeRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record StripeSetupIntentRequest(
    string CustomerEmail,
    string? CustomerName,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record StripePaymentIntentRequest(
    string CustomerEmail,
    string? CustomerName,
    decimal Amount,
    string Currency,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record StripeSetupIntentResult(
    string PublishableKey,
    string ClientSecret,
    string CustomerId,
    string SetupIntentId);

public sealed record StripePaymentIntentResult(
    string PublishableKey,
    string ClientSecret,
    string CustomerId,
    string PaymentIntentId);

public sealed record StripeRetrievedSetupIntentResult(
    string SetupIntentId,
    string Status,
    string? CustomerId,
    string? PaymentMethodId,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record StripeRetrievedPaymentIntentResult(
    string PaymentIntentId,
    string Status,
    string? CustomerId,
    string? PaymentMethodId,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record StripeOffSessionChargeRequest(
    string CustomerId,
    string PaymentMethodId,
    decimal Amount,
    string Currency,
    IReadOnlyDictionary<string, string> Metadata);
