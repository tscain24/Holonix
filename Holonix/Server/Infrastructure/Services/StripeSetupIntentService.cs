using Holonix.Server.Application.Interfaces;
using Holonix.Server.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Stripe;

namespace Holonix.Server.Infrastructure.Services;

public sealed class StripeSetupIntentService : IStripeSetupIntentService
{
    private readonly StripeOptions _stripeOptions;
    private readonly StripeClient _stripeClient;

    public StripeSetupIntentService(IOptions<StripeOptions> options)
    {
        _stripeOptions = options.Value;
        if (string.IsNullOrWhiteSpace(_stripeOptions.SecretKey))
        {
            throw new InvalidOperationException("Stripe secret key is missing.");
        }

        if (string.IsNullOrWhiteSpace(_stripeOptions.PublishableKey))
        {
            throw new InvalidOperationException("Stripe publishable key is missing.");
        }

        _stripeClient = new StripeClient(_stripeOptions.SecretKey);
    }

    public async Task<StripeSetupIntentResult> CreateSetupIntentAsync(
        StripeSetupIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        var metadata = request.Metadata.ToDictionary(pair => pair.Key, pair => pair.Value);

        var customerService = new CustomerService(_stripeClient);
        var customer = await customerService.CreateAsync(new CustomerCreateOptions
        {
            Email = request.CustomerEmail,
            Name = request.CustomerName,
            Metadata = metadata,
        }, cancellationToken: cancellationToken);

        var setupIntentService = new SetupIntentService(_stripeClient);
        var setupIntent = await setupIntentService.CreateAsync(new SetupIntentCreateOptions
        {
            Customer = customer.Id,
            Usage = "off_session",
            Metadata = metadata,
            Description = $"Holonix booking setup for {request.CustomerEmail}",
        }, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(setupIntent.ClientSecret))
        {
            throw new InvalidOperationException("Stripe setup intent client secret was not returned.");
        }

        return new StripeSetupIntentResult(
            _stripeOptions.PublishableKey,
            setupIntent.ClientSecret,
            customer.Id,
            setupIntent.Id);
    }
}
