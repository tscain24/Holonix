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
            AutomaticPaymentMethods = new SetupIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
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
            new PaymentIntentGetOptions
            {
                Expand = ["latest_charge"],
            },
            requestOptions: null,
            cancellationToken: cancellationToken);

        return await BuildPaymentIntentResultAsync(paymentIntent, cancellationToken);
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

        if (string.IsNullOrWhiteSpace(request.DestinationAccountId))
        {
            throw new ArgumentException("Destination connected account id is required.", nameof(request));
        }

        var metadata = request.Metadata.ToDictionary(pair => pair.Key, pair => pair.Value);
        var workloadId = request.Metadata.TryGetValue("workloadId", out var value) ? value : null;
        var operationKey = string.IsNullOrWhiteSpace(workloadId)
            ? null
            : BookingPaymentIdempotencyKeys.ForWorkload(workloadId);
        var paymentIntentService = new PaymentIntentService(_stripeClient);
        var createOptions = StripeDestinationChargeOptionsFactory.Create(request, metadata);

        PaymentIntent paymentIntent;
        try
        {
            paymentIntent = await paymentIntentService.CreateAsync(
                createOptions,
                new RequestOptions { IdempotencyKey = operationKey is null ? null : $"{operationKey}-create" },
                cancellationToken);
        }
        catch (StripeException) when (operationKey is not null)
        {
            // A network failure can hide a successful Stripe create. Repeating the same
            // idempotent request returns that PaymentIntent rather than creating a second one.
            paymentIntent = await paymentIntentService.CreateAsync(
                createOptions,
                new RequestOptions { IdempotencyKey = $"{operationKey}-create" },
                cancellationToken);
        }

        if (!string.Equals(paymentIntent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                paymentIntent = await paymentIntentService.ConfirmAsync(
                    paymentIntent.Id,
                    new PaymentIntentConfirmOptions
                    {
                        PaymentMethod = request.PaymentMethodId,
                        OffSession = true,
                    },
                    new RequestOptions { IdempotencyKey = operationKey is null ? null : $"{operationKey}-confirm" },
                    cancellationToken);
            }
            catch (StripeException)
            {
                // Confirmation may have succeeded even when its response was lost. Always
                // reconcile the known PaymentIntent before allowing a caller to retry.
                var reconciled = await paymentIntentService.GetAsync(
                    paymentIntent.Id,
                    new PaymentIntentGetOptions(),
                    requestOptions: null,
                    cancellationToken: cancellationToken);
                // Returning the reconciled non-success state also lets Holonix persist the
                // PaymentIntent id for webhook correlation and safe operator retries.
                paymentIntent = reconciled;
            }
        }

        return await BuildPaymentIntentResultAsync(paymentIntent, cancellationToken);
    }

    private async Task<StripeRetrievedPaymentIntentResult> BuildPaymentIntentResultAsync(
        PaymentIntent paymentIntent,
        CancellationToken cancellationToken)
    {
        Charge? charge = paymentIntent.LatestCharge;
        if (charge is null && !string.IsNullOrWhiteSpace(paymentIntent.LatestChargeId))
        {
            charge = await new ChargeService(_stripeClient).GetAsync(
                paymentIntent.LatestChargeId,
                new ChargeGetOptions(),
                requestOptions: null,
                cancellationToken: cancellationToken);
        }

        return new StripeRetrievedPaymentIntentResult(
            paymentIntent.Id,
            paymentIntent.Status ?? string.Empty,
            paymentIntent.CustomerId,
            paymentIntent.PaymentMethodId,
            paymentIntent.Metadata ?? new Dictionary<string, string>(StringComparer.Ordinal),
            charge?.Id ?? paymentIntent.LatestChargeId,
            charge?.TransferId,
            charge?.ApplicationFeeId);
    }

    private static long ConvertToStripeAmount(decimal amount)
    {
        return (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
    }
}

public static class BookingPaymentIdempotencyKeys
{
    public static string ForWorkload(string workloadId) => $"holonix-booking-charge-{workloadId}-v1";
}
