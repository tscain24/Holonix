using Holonix.Server.Application.Interfaces;
using Holonix.Server.Application.Services;
using Stripe;

namespace Holonix.Server.Infrastructure.Services;

public static class StripeDestinationChargeOptionsFactory
{
    public static PaymentIntentCreateOptions Create(
        StripeOffSessionChargeRequest request,
        Dictionary<string, string> metadata)
    {
        var allocation = BookingPaymentAllocation.Calculate(
            request.Amount,
            request.Amount - request.ApplicationFeeAmount);

        return new PaymentIntentCreateOptions
        {
            Amount = allocation.CustomerTotal,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "usd" : request.Currency.Trim().ToLowerInvariant(),
            Customer = request.CustomerId,
            PaymentMethod = request.PaymentMethodId,
            Metadata = metadata,
            Description = "Holonix accepted booking charge",
            ApplicationFeeAmount = allocation.PlatformFeeAmount,
            TransferData = new PaymentIntentTransferDataOptions
            {
                Destination = request.DestinationAccountId,
            },
        };
    }
}
