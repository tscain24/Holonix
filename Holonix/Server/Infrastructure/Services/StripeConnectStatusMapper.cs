using Holonix.Server.Domain.Entities;
using Stripe.V2.Core;

namespace Holonix.Server.Infrastructure.Services;

public static class StripeConnectStatusMapper
{
    public static string Map(Account account)
    {
        if (account.Closed == true)
        {
            return BusinessPaymentAccountStatuses.ActionRequired;
        }

        var balance = account.Configuration?.Recipient?.Capabilities?.StripeBalance;
        var transfersStatus = balance?.StripeTransfers?.Status;
        var payoutsStatus = balance?.Payouts?.Status;

        if (IsActive(transfersStatus) && IsActive(payoutsStatus))
        {
            return BusinessPaymentAccountStatuses.Ready;
        }

        var requirements = account.Requirements?.Entries ?? [];
        if (requirements.Any(entry => entry.Errors?.Count > 0))
        {
            return BusinessPaymentAccountStatuses.ActionRequired;
        }

        if (requirements.Any(entry =>
                string.Equals(entry.AwaitingActionFrom, "user", StringComparison.OrdinalIgnoreCase)))
        {
            return BusinessPaymentAccountStatuses.SetupIncomplete;
        }

        return BusinessPaymentAccountStatuses.VerificationPending;
    }

    private static bool IsActive(string? value) =>
        string.Equals(value, "active", StringComparison.OrdinalIgnoreCase);
}
