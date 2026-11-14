using Holonix.Server.Application.Interfaces;
using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Application.Services;

public static class StripeConnectPaymentReadiness
{
    public static StripeConnectPaymentReadinessResult Evaluate(
        string? connectedAccountId,
        StripeConnectAccountResult? account)
    {
        if (string.IsNullOrWhiteSpace(connectedAccountId) || account is null)
        {
            return new(false, "business_payment_account_not_connected", "The business must connect Stripe before this booking can be charged.");
        }

        if (!string.Equals(account.AccountId, connectedAccountId, StringComparison.Ordinal))
        {
            return new(false, "business_payment_account_mismatch", "The business Stripe account could not be verified. Reconnect Stripe before charging this booking.");
        }

        if (!string.Equals(account.TransfersCapabilityStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            return new(false, "business_transfers_not_active", "The business must finish Stripe verification so it can receive transfers before this booking can be charged.");
        }

        if (!string.Equals(account.PayoutsCapabilityStatus, "active", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(account.Status, BusinessPaymentAccountStatuses.Ready, StringComparison.OrdinalIgnoreCase))
        {
            return new(false, "business_payouts_not_active", "The business must finish Stripe payout setup before this booking can be charged.");
        }

        return new(true, null, null);
    }
}

public sealed record StripeConnectPaymentReadinessResult(bool IsReady, string? FailureCode, string? FailureMessage);
