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

    public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(
        StripePaymentIntentRequest request,
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

        var paymentIntentService = new PaymentIntentService(_stripeClient);
        var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount = ConvertToStripeAmount(request.Amount),
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "usd" : request.Currency.Trim().ToLowerInvariant(),
            Customer = customer.Id,
            Metadata = metadata,
            Description = $"Holonix booking payment for {request.CustomerEmail}",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
        }, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(paymentIntent.ClientSecret))
        {
            throw new InvalidOperationException("Stripe payment intent client secret was not returned.");
        }

        return new StripePaymentIntentResult(
            _stripeOptions.PublishableKey,
            paymentIntent.ClientSecret,
            customer.Id,
            paymentIntent.Id);
    }

    public async Task<StripeRetrievedSetupIntentResult> GetSetupIntentAsync(
        string setupIntentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(setupIntentId))
        {
            throw new ArgumentException("Setup intent id is required.", nameof(setupIntentId));
        }

        var setupIntentService = new SetupIntentService(_stripeClient);
        var setupIntent = await setupIntentService.GetAsync(
            setupIntentId,
            new SetupIntentGetOptions(),
            requestOptions: null,
            cancellationToken: cancellationToken);

        return new StripeRetrievedSetupIntentResult(
            setupIntent.Id,
            setupIntent.Status ?? string.Empty,
            setupIntent.CustomerId,
            setupIntent.PaymentMethodId,
            setupIntent.Metadata ?? new Dictionary<string, string>(StringComparer.Ordinal));
    }

    public async Task<StripeRetrievedPaymentIntentResult> GetPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            throw new ArgumentException("Payment intent id is required.", nameof(paymentIntentId));
        }

        var paymentIntentService = new PaymentIntentService(_stripeClient);
        var paymentIntent = await paymentIntentService.GetAsync(
            paymentIntentId,
            new PaymentIntentGetOptions(),
            requestOptions: null,
            cancellationToken: cancellationToken);

        return new StripeRetrievedPaymentIntentResult(
            paymentIntent.Id,
            paymentIntent.Status ?? string.Empty,
            paymentIntent.CustomerId,
            paymentIntent.PaymentMethodId,
            paymentIntent.Metadata ?? new Dictionary<string, string>(StringComparer.Ordinal));
    }

    public async Task<StripeRetrievedPaymentIntentResult> ChargeSavedPaymentMethodAsync(
        StripeOffSessionChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            throw new ArgumentException("Customer id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
        {
            throw new ArgumentException("Payment method id is required.", nameof(request));
        }

        var metadata = request.Metadata.ToDictionary(pair => pair.Key, pair => pair.Value);
        var paymentIntentService = new PaymentIntentService(_stripeClient);
        var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount = ConvertToStripeAmount(request.Amount),
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "usd" : request.Currency.Trim().ToLowerInvariant(),
            Customer = request.CustomerId,
            PaymentMethod = request.PaymentMethodId,
            Confirm = true,
            OffSession = true,
            ErrorOnRequiresAction = true,
            Metadata = metadata,
            Description = "Holonix accepted booking charge",
        }, cancellationToken: cancellationToken);

        return new StripeRetrievedPaymentIntentResult(
            paymentIntent.Id,
            paymentIntent.Status ?? string.Empty,
            paymentIntent.CustomerId,
            paymentIntent.PaymentMethodId,
            paymentIntent.Metadata ?? new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private static long ConvertToStripeAmount(decimal amount)
    {
        return (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
    }
}
