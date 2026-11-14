namespace Holonix.Server.Application.Services;

public static class BookingPaymentTiming
{
    public static BookingPaymentTimingDecision Decide(DateTime serviceStartUtc, DateTime nowUtc)
    {
        if (serviceStartUtc <= nowUtc)
        {
            return new BookingPaymentTimingDecision(false, false, null);
        }

        var scheduledForUtc = serviceStartUtc.AddHours(-24);
        return scheduledForUtc > nowUtc
            ? new BookingPaymentTimingDecision(true, false, scheduledForUtc)
            : new BookingPaymentTimingDecision(true, true, nowUtc);
    }
}

public sealed record BookingPaymentTimingDecision(
    bool CanAccept,
    bool ChargeImmediately,
    DateTime? ChargeScheduledForUtc);
