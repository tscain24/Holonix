using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Services;
using Stripe.V2.Core;
using Xunit;

namespace Holonix.Server.Tests;

public class StripeConnectStatusMapperTests
{
    [Fact]
    public void Map_ReturnsReadyOnlyWhenTransfersAndPayoutsAreActive()
    {
        var account = AccountWithCapabilities("active", "active");

        Assert.Equal(BusinessPaymentAccountStatuses.Ready, StripeConnectStatusMapper.Map(account));

        account.Configuration.Recipient.Capabilities.StripeBalance.Payouts.Status = "pending";
        Assert.Equal(BusinessPaymentAccountStatuses.VerificationPending, StripeConnectStatusMapper.Map(account));
    }

    [Fact]
    public void Map_ReturnsSetupIncompleteWhenStripeNeedsUserInput()
    {
        var account = AccountWithCapabilities("pending", "pending");
        account.Requirements = new AccountRequirements
        {
            Entries = [new AccountRequirementsEntry { AwaitingActionFrom = "user" }],
        };

        Assert.Equal(BusinessPaymentAccountStatuses.SetupIncomplete, StripeConnectStatusMapper.Map(account));
    }

    [Fact]
    public void Map_ReturnsActionRequiredWhenStripeReportsRequirementErrors()
    {
        var account = AccountWithCapabilities("restricted", "restricted");
        account.Requirements = new AccountRequirements
        {
            Entries =
            [
                new AccountRequirementsEntry
                {
                    AwaitingActionFrom = "user",
                    Errors = [new AccountRequirementsEntryError()],
                },
            ],
        };

        Assert.Equal(BusinessPaymentAccountStatuses.ActionRequired, StripeConnectStatusMapper.Map(account));
    }

    [Fact]
    public void Map_ReturnsActionRequiredWhenTheStripeAccountIsClosed()
    {
        var account = AccountWithCapabilities("inactive", "inactive");
        account.Closed = true;

        Assert.Equal(BusinessPaymentAccountStatuses.ActionRequired, StripeConnectStatusMapper.Map(account));
    }

    private static Account AccountWithCapabilities(string transfers, string payouts) => new()
    {
        Configuration = new AccountConfiguration
        {
            Recipient = new AccountConfigurationRecipient
            {
                Capabilities = new AccountConfigurationRecipientCapabilities
                {
                    StripeBalance = new AccountConfigurationRecipientCapabilitiesStripeBalance
                    {
                        StripeTransfers = new AccountConfigurationRecipientCapabilitiesStripeBalanceStripeTransfers { Status = transfers },
                        Payouts = new AccountConfigurationRecipientCapabilitiesStripeBalancePayouts { Status = payouts },
                    },
                },
            },
        },
        Requirements = new AccountRequirements { Entries = [] },
    };
}
