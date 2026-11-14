using Holonix.Server.Application.Services;
using Holonix.Server.Infrastructure.Services;
using Holonix.Server.Domain.Entities;
using Xunit;

namespace Holonix.Server.Tests;

public sealed class BookingPaymentTimingTests
{
    private static readonly DateTime NowUtc = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MoreThanTwentyFourHoursAway_SchedulesExactlyTwentyFourHoursBeforeService()
    {
        var start = NowUtc.AddHours(30);

        var result = BookingPaymentTiming.Decide(start, NowUtc);

        Assert.True(result.CanAccept);
        Assert.False(result.ChargeImmediately);
        Assert.Equal(start.AddHours(-24), result.ChargeScheduledForUtc);
    }

    [Theory]
    [InlineData(24)]
    [InlineData(1)]
    [InlineData(0.01)]
    public void TwentyFourHoursOrLessAway_ChargesImmediately(double hoursUntilStart)
    {
        var result = BookingPaymentTiming.Decide(NowUtc.AddHours(hoursUntilStart), NowUtc);

        Assert.True(result.CanAccept);
        Assert.True(result.ChargeImmediately);
        Assert.Equal(NowUtc, result.ChargeScheduledForUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PassedOrStartingNow_CannotBeAccepted(double hoursUntilStart)
    {
        var result = BookingPaymentTiming.Decide(NowUtc.AddHours(hoursUntilStart), NowUtc);

        Assert.False(result.CanAccept);
        Assert.False(result.ChargeImmediately);
        Assert.Null(result.ChargeScheduledForUtc);
    }

    [Fact]
    public void WorkloadIdempotencyKey_IsStableAcrossAcceptanceSchedulerAndRetry()
    {
        var acceptance = BookingPaymentIdempotencyKeys.ForWorkload("4821");
        var scheduler = BookingPaymentIdempotencyKeys.ForWorkload("4821");
        var retry = BookingPaymentIdempotencyKeys.ForWorkload("4821");

        Assert.Equal(acceptance, scheduler);
        Assert.Equal(acceptance, retry);
        Assert.NotEqual(acceptance, BookingPaymentIdempotencyKeys.ForWorkload("4822"));
    }

    [Fact]
    public async Task ConcurrentChargeCallers_DeriveOneLogicalStripeOperation()
    {
        var keys = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(
            () => BookingPaymentIdempotencyKeys.ForWorkload("4821"))));

        Assert.Single(keys.Distinct());
    }

    [Fact]
    public void FailureReconciliation_DoesNotDowngradeAnAlreadySucceededPayment()
    {
        var payment = new BookingPayment { PaymentStatus = BookingPaymentStatusNames.ChargeSucceeded };

        BookingPaymentStatusReconciler.MarkFailed(payment, NowUtc, "late_failure", "Out-of-order webhook");

        Assert.Equal(BookingPaymentStatusNames.ChargeSucceeded, payment.PaymentStatus);
        Assert.Null(payment.LastPaymentFailureUtc);
    }

    [Fact]
    public void FailureReconciliation_MarksUnpaidBookingFailed()
    {
        var payment = new BookingPayment { PaymentStatus = BookingPaymentStatusNames.ChargePending };

        BookingPaymentStatusReconciler.MarkFailed(payment, NowUtc, "card_declined", "Declined");

        Assert.Equal(BookingPaymentStatusNames.ChargeFailed, payment.PaymentStatus);
        Assert.Equal("card_declined", payment.LastPaymentFailureCode);
        Assert.Null(payment.ChargedAtUtc);
    }
}
