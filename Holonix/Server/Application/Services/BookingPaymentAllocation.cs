namespace Holonix.Server.Application.Services;

public static class BookingPaymentAllocation
{
    public static BookingPaymentAllocationResult Calculate(decimal customerTotal, decimal businessAmount)
    {
        if (customerTotal < 0 || businessAmount < 0 || businessAmount > customerTotal)
        {
            throw new ArgumentOutOfRangeException(nameof(customerTotal), "Payment allocation amounts are invalid.");
        }

        return new BookingPaymentAllocationResult(
            ToStripeAmount(customerTotal),
            ToStripeAmount(businessAmount),
            ToStripeAmount(customerTotal - businessAmount));
    }

    private static long ToStripeAmount(decimal amount) =>
        (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
}

public sealed record BookingPaymentAllocationResult(
    long CustomerTotal,
    long BusinessAmount,
    long PlatformFeeAmount);
