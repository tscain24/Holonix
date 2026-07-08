namespace Holonix.Server.Application.Interfaces;

public interface IStripeSetupIntentService
{
    Task<StripeSetupIntentResult> CreateSetupIntentAsync(
        StripeSetupIntentRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record StripeSetupIntentRequest(
    string CustomerEmail,
    string? CustomerName,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record StripeSetupIntentResult(
    string PublishableKey,
    string ClientSecret,
    string CustomerId,
    string SetupIntentId);
