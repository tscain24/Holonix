using Holonix.Server.Application.Interfaces;
using Stripe.V2.Core;

namespace Holonix.Server.Infrastructure.Services;

public static class StripeConnectAccountOptionsFactory
{
    public static AccountCreateOptions Create(StripeConnectAccountRequest request)
    {
        var countryCode = NormalizeCountryCode(request.CountryCode);

        return new AccountCreateOptions
        {
            ContactEmail = NullIfEmpty(request.BusinessEmail),
            ContactPhone = NullIfEmpty(request.BusinessPhoneNumber),
            Dashboard = "express",
            DisplayName = request.BusinessName,
            Identity = new AccountCreateIdentityOptions { Country = countryCode },
            Metadata = new Dictionary<string, string>
            {
                ["holonix_business_code"] = request.BusinessCode,
            },
            Defaults = new AccountCreateDefaultsOptions
            {
                Responsibilities = new AccountCreateDefaultsResponsibilitiesOptions
                {
                    FeesCollector = "application",
                    LossesCollector = "application",
                },
            },
            Configuration = new AccountCreateConfigurationOptions
            {
                Recipient = new AccountCreateConfigurationRecipientOptions
                {
                    Capabilities = new AccountCreateConfigurationRecipientCapabilitiesOptions
                    {
                        StripeBalance = new AccountCreateConfigurationRecipientCapabilitiesStripeBalanceOptions
                        {
                            StripeTransfers = new AccountCreateConfigurationRecipientCapabilitiesStripeBalanceStripeTransfersOptions
                            {
                                Requested = true,
                            },
                        },
                    },
                },
            },
            Include =
            [
                "configuration.recipient",
                "requirements",
                "future_requirements",
            ],
        };
    }

    public static string NormalizeCountryCode(string? value)
    {
        var countryCode = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (countryCode.Length != 2 || countryCode.Any(character => character is < 'A' or > 'Z'))
        {
            throw new StripeConnectValidationException(
                "A supported business country is required before connecting Stripe.");
        }

        return countryCode;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
