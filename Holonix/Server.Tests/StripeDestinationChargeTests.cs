using Holonix.Server.Application.Interfaces;
using Holonix.Server.Application.Services;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Services;
using Xunit;

namespace Holonix.Server.Tests;

public sealed class StripeDestinationChargeTests
{
    [Fact]
    public void OneHundredDollarService_Charges105_Transfers100_Retains5GrossFee()
    {
        var allocation = BookingPaymentAllocation.Calculate(105m, 100m);
        var request = new StripeOffSessionChargeRequest(
            "cus_test",
            "pm_test",
            105m,
            "usd",
            new Dictionary<string, string> { ["workloadId"] = "42" },
            "acct_business",
            5m);

        var options = StripeDestinationChargeOptionsFactory.Create(request, request.Metadata.ToDictionary());

        Assert.Equal(10500, allocation.CustomerTotal);
        Assert.Equal(10000, allocation.BusinessAmount);
        Assert.Equal(500, allocation.PlatformFeeAmount);
        Assert.Equal(10500, options.Amount);
        Assert.Equal(500, options.ApplicationFeeAmount);
        Assert.Equal("acct_business", options.TransferData.Destination);
    }

    [Theory]
    [InlineData(null, null, "business_payment_account_not_connected")]
    [InlineData("pending", "active", "business_transfers_not_active")]
    [InlineData("active", "pending", "business_payouts_not_active")]
    public void UnreadyConnectedAccounts_AreBlockedWithActionableReason(
        string? transfers,
        string? payouts,
        string expectedCode)
    {
        var account = transfers is null && payouts is null
            ? null
            : new StripeConnectAccountResult(
                "acct_business",
                BusinessPaymentAccountStatuses.VerificationPending,
                transfers,
                payouts);

        var result = StripeConnectPaymentReadiness.Evaluate(
            account is null ? null : "acct_business",
            account);

        Assert.False(result.IsReady);
        Assert.Equal(expectedCode, result.FailureCode);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureMessage));
    }

    [Fact]
    public void ActiveRecipientAndPayoutCapabilities_ArePaymentReady()
    {
        var account = new StripeConnectAccountResult(
            "acct_business",
            BusinessPaymentAccountStatuses.Ready,
            "active",
            "active");

        Assert.True(StripeConnectPaymentReadiness.Evaluate("acct_business", account).IsReady);
    }

    [Fact]
    public void DuplicateAndOutOfOrderWebhookReconciliation_CannotDowngradePaidDistribution()
    {
        var payment = new BookingPayment { PaymentStatus = BookingPaymentStatusNames.DistributionPending };
        var now = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

        BookingPaymentStatusReconciler.MarkSucceeded(payment, now);
        BookingPaymentStatusReconciler.MarkSucceeded(payment, now.AddMinutes(1));
        BookingPaymentStatusReconciler.MarkFailed(payment, now.AddMinutes(2), "late_failure", "duplicate webhook");

        Assert.Equal(BookingPaymentStatusNames.ChargeSucceeded, payment.PaymentStatus);
        Assert.Equal(now, payment.ChargedAtUtc);
        Assert.Null(payment.LastPaymentFailureCode);
    }

    [Fact]
    public void PaymentFailureWebhook_CannotHideAnExistingTransferFailure()
    {
        var payment = new BookingPayment
        {
            PaymentStatus = BookingPaymentStatusNames.DistributionFailed,
            LastPaymentFailureCode = "stripe_transfer_failed",
        };

        BookingPaymentStatusReconciler.MarkFailed(
            payment,
            new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc),
            "card_declined",
            "out-of-order event");

        Assert.Equal(BookingPaymentStatusNames.DistributionFailed, payment.PaymentStatus);
        Assert.Equal("stripe_transfer_failed", payment.LastPaymentFailureCode);
    }

    [Fact]
    public void RetryUsesSameLogicalChargeKeyAndNoSeparateTransferKey()
    {
        Assert.Equal(
            BookingPaymentIdempotencyKeys.ForWorkload("42"),
            BookingPaymentIdempotencyKeys.ForWorkload("42"));
    }
}
