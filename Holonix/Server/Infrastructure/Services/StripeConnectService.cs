using Holonix.Server.Application.Interfaces;
using Holonix.Server.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Events;
using Stripe.V2.Core;
using System.Net;
using StripeV2Account = Stripe.V2.Core.Account;

namespace Holonix.Server.Infrastructure.Services;

public sealed class StripeConnectService : IStripeConnectService
{
    private readonly StripeOptions _options;
    private readonly StripeClient? _client;
    private readonly bool _requireTestMode;

    public StripeConnectService(IOptions<StripeOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _requireTestMode = environment.IsDevelopment();
        _client = string.IsNullOrWhiteSpace(_options.SecretKey) ? null : new StripeClient(_options.SecretKey);
    }

    public async Task<StripeConnectAccountResult> CreateAccountAsync(
        StripeConnectAccountRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var options = StripeConnectAccountOptionsFactory.Create(request);
        await EnsureCountrySupportedAsync(options.Identity.Country, cancellationToken);

        var account = await Client.V2.Core.Accounts.CreateAsync(
            options,
            new RequestOptions { IdempotencyKey = idempotencyKey },
            cancellationToken);

        EnsureAllowedMode(account);
        return Map(account);
    }

    public async Task<StripeConnectAccountResult> GetAccountAsync(
        string connectedAccountId,
        CancellationToken cancellationToken = default)
    {
        var account = await Client.V2.Core.Accounts.GetAsync(
            connectedAccountId,
            new Stripe.V2.Core.AccountGetOptions { Include = AccountIncludes },
            cancellationToken: cancellationToken);

        EnsureAllowedMode(account);
        return Map(account);
    }

    public async Task<string> CreateAccountLinkAsync(
        string connectedAccountId,
        string returnUrl,
        string refreshUrl,
        bool updateExistingAccount,
        CancellationToken cancellationToken = default)
    {
        var link = await Client.V2.Core.AccountLinks.CreateAsync(
            StripeConnectAccountLinkOptionsFactory.Create(
                connectedAccountId,
                returnUrl,
                refreshUrl,
                updateExistingAccount),
            cancellationToken: cancellationToken);

        return link.Url;
    }

    public string? ParseRelatedAccountId(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectWebhookSecret))
        {
            throw new InvalidOperationException("Stripe Connect webhook secret is missing.");
        }

        var notification = Client.ParseEventNotification(
            payload,
            signatureHeader,
            _options.ConnectWebhookSecret,
            tolerance: 300);

        return notification switch
        {
            V2CoreAccountUpdatedEventNotification value => value.RelatedObject?.Id,
            V2CoreAccountIncludingConfigurationRecipientUpdatedEventNotification value => value.RelatedObject?.Id,
            V2CoreAccountIncludingConfigurationRecipientCapabilityStatusUpdatedEventNotification value => value.RelatedObject?.Id,
            V2CoreAccountIncludingRequirementsUpdatedEventNotification value => value.RelatedObject?.Id,
            V2CoreAccountIncludingFutureRequirementsUpdatedEventNotification value => value.RelatedObject?.Id,
            _ => null,
        };
    }

    private static readonly List<string> AccountIncludes =
    [
        "configuration.recipient",
        "requirements",
        "future_requirements",
    ];

    private async Task EnsureCountrySupportedAsync(string countryCode, CancellationToken cancellationToken)
    {
        try
        {
            await Client.V1.CountrySpecs.GetAsync(
                countryCode,
                new CountrySpecGetOptions(),
                cancellationToken: cancellationToken);
        }
        catch (StripeException ex) when (
            ex.HttpStatusCode == HttpStatusCode.NotFound ||
            string.Equals(ex.StripeError?.Code, "resource_missing", StringComparison.OrdinalIgnoreCase))
        {
            throw new StripeConnectValidationException(
                $"Stripe Connect is not supported for the business country '{countryCode}'.");
        }
    }

    private static StripeConnectAccountResult Map(StripeV2Account account)
    {
        var balance = account.Configuration?.Recipient?.Capabilities?.StripeBalance;
        return new StripeConnectAccountResult(
            account.Id,
            StripeConnectStatusMapper.Map(account),
            balance?.StripeTransfers?.Status,
            balance?.Payouts?.Status);
    }

    private StripeClient Client
    {
        get
        {
            if (_client is null)
            {
                throw new InvalidOperationException("Stripe secret key is missing.");
            }

            if (_requireTestMode &&
                !_options.SecretKey.StartsWith("sk_test_", StringComparison.Ordinal) &&
                !_options.SecretKey.StartsWith("rk_test_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Development must use a Stripe test-mode key.");
            }

            return _client;
        }
    }

    private void EnsureAllowedMode(StripeV2Account account)
    {
        if (_requireTestMode && account.Livemode)
        {
            throw new InvalidOperationException("Stripe Connect onboarding must use test mode during development.");
        }
    }

}
