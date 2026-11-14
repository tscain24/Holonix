using Holonix.Server.Domain.Entities;
using Holonix.Server.Application.Services;
using Holonix.Server.Application.Interfaces;
using Holonix.Server.Infrastructure.Configuration;
using Holonix.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace Holonix.Server.Controllers;

[ApiController]
[Route("api/stripe/payments/webhook")]
public sealed class StripePaymentWebhookController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly StripeOptions _options;
    private readonly IStripeSetupIntentService _stripe;
    private readonly ILogger<StripePaymentWebhookController> _logger;

    public StripePaymentWebhookController(
        ApplicationDbContext dbContext,
        IOptions<StripeOptions> options,
        IStripeSetupIntentService stripe,
        ILogger<StripePaymentWebhookController> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _stripe = stripe;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        string payload;
        using (var reader = new StreamReader(Request.Body))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                Request.Headers["Stripe-Signature"],
                _options.WebhookSecret,
                tolerance: 300);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Rejected Stripe payment webhook with an invalid signature.");
            return BadRequest();
        }

        if (stripeEvent.Type == "transfer.failed" && stripeEvent.Data.Object is Transfer failedTransfer)
        {
            var failedPayment = await _dbContext.BookingPayments
                .SingleOrDefaultAsync(item => item.StripeTransferId == failedTransfer.Id, cancellationToken);
            if (failedPayment is not null)
            {
                failedPayment.PaymentStatus = BookingPaymentStatusNames.DistributionFailed;
                failedPayment.LastPaymentFailureUtc = DateTime.UtcNow;
                failedPayment.LastPaymentFailureCode = "stripe_transfer_failed";
                failedPayment.LastPaymentFailureMessage = "Stripe reported that the business transfer failed. The booking is not marked paid.";
                failedPayment.UpdatedDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return Ok();
        }

        if (stripeEvent.Data.Object is not PaymentIntent intent ||
            (stripeEvent.Type != "payment_intent.succeeded" && stripeEvent.Type != "payment_intent.payment_failed"))
        {
            return Ok();
        }

        var payment = await _dbContext.BookingPayments
            .SingleOrDefaultAsync(item => item.StripePaymentIntentId == intent.Id, cancellationToken);
        if (payment is null)
        {
            return Ok();
        }

        if (stripeEvent.Type == "payment_intent.succeeded")
        {
            if (payment.PaymentStatus == BookingPaymentStatusNames.DistributionFailed)
            {
                return Ok();
            }

            var reconciled = await _stripe.GetPaymentIntentAsync(intent.Id, cancellationToken);
            payment.StripeChargeId = reconciled.ChargeId;
            payment.StripeTransferId = reconciled.TransferId;
            payment.StripeApplicationFeeId = reconciled.ApplicationFeeId;
            if (string.IsNullOrWhiteSpace(reconciled.ChargeId) ||
                string.IsNullOrWhiteSpace(reconciled.TransferId) ||
                string.IsNullOrWhiteSpace(reconciled.ApplicationFeeId))
            {
                payment.PaymentStatus = BookingPaymentStatusNames.DistributionPending;
                payment.LastPaymentFailureUtc = DateTime.UtcNow;
                payment.LastPaymentFailureCode = "stripe_distribution_pending";
                payment.LastPaymentFailureMessage = "The customer payment succeeded, but Stripe distribution identifiers are still being reconciled.";
                payment.UpdatedDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            BookingPaymentStatusReconciler.MarkSucceeded(payment, DateTime.UtcNow);
            var paymentFailedStatusId = await _dbContext.WorkloadStatuses
                .Where(item => item.WorkloadType.Type == WorkloadTypeNames.CustomerBooking && item.Status == WorkloadStatusNames.PaymentFailed)
                .Select(item => item.WorkloadStatusId)
                .SingleOrDefaultAsync(cancellationToken);
            if (paymentFailedStatusId != 0)
            {
                var workload = await _dbContext.Workloads.SingleAsync(item => item.WorkloadId == payment.WorkloadId, cancellationToken);
                if (workload.WorkloadStatusId == paymentFailedStatusId)
                {
                    workload.WorkloadStatusId = await _dbContext.WorkloadStatuses
                        .Where(item => item.WorkloadType.Type == WorkloadTypeNames.CustomerBooking && item.Status == WorkloadStatusNames.AwaitingAcceptance)
                        .Select(item => item.WorkloadStatusId)
                        .SingleAsync(cancellationToken);
                }
            }
        }
        else
        {
            BookingPaymentStatusReconciler.MarkFailed(
                payment,
                DateTime.UtcNow,
                intent.LastPaymentError?.Code,
                intent.LastPaymentError?.Message ?? "Stripe reported that the payment failed.");
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok();
    }
}
