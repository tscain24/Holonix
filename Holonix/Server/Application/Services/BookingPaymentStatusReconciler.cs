using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Application.Services;

public static class BookingPaymentStatusReconciler
{
    public static void MarkSucceeded(BookingPayment payment, DateTime nowUtc)
    {
        payment.PaymentStatus = BookingPaymentStatusNames.ChargeSucceeded;
        payment.ChargedAtUtc ??= nowUtc;
        payment.LastPaymentFailureUtc = null;
        payment.LastPaymentFailureCode = null;
        payment.LastPaymentFailureMessage = null;
        payment.UpdatedDate = nowUtc;
    }

    public static void MarkFailed(BookingPayment payment, DateTime nowUtc, string? code, string message)
    {
        if (string.Equals(payment.PaymentStatus, BookingPaymentStatusNames.ChargeSucceeded, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(payment.PaymentStatus, BookingPaymentStatusNames.DistributionFailed, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        payment.PaymentStatus = BookingPaymentStatusNames.ChargeFailed;
        payment.LastPaymentFailureUtc = nowUtc;
        payment.LastPaymentFailureCode = code;
        payment.LastPaymentFailureMessage = message;
        payment.UpdatedDate = nowUtc;
    }
}
