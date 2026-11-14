using Holonix.Server.Application.Interfaces;
using Holonix.Server.Infrastructure.Services;
using Xunit;

namespace Holonix.Server.Tests;

public class StripeConnectAccountOptionsFactoryTests
{
    [Fact]
    public void Create_SetsNormalizedPersistedCountryBeforeRecipientConfiguration()
    {
        var options = StripeConnectAccountOptionsFactory.Create(
            new StripeConnectAccountRequest("HOLONIX001", "Holonix Services", null, null, " us "));

        Assert.Equal("US", options.Identity.Country);
        Assert.True(options.Configuration.Recipient.Capabilities.StripeBalance.StripeTransfers.Requested);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("USA")]
    [InlineData("1A")]
    public void Create_RejectsMissingOrInvalidCountryCodes(string? countryCode)
    {
        var exception = Assert.Throws<StripeConnectValidationException>(() =>
            StripeConnectAccountOptionsFactory.Create(
                new StripeConnectAccountRequest("HOLONIX001", "Holonix Services", null, null, countryCode)));

        Assert.Contains("business country", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
