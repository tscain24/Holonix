using System.Globalization;
using Holonix.Server.Application.Interfaces;
using Holonix.Server.Application.Services;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Holonix.Server.Infrastructure.Services;

public sealed class BookingPaymentProcessor : IBookingPaymentProcessor
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IStripeSetupIntentService _stripe;
    private readonly IStripeConnectService _stripeConnect;
    private readonly ILogger<BookingPaymentProcessor> _logger;

    public BookingPaymentProcessor(
        ApplicationDbContext dbContext,
        IStripeSetupIntentService stripe,
        IStripeConnectService stripeConnect,
        ILogger<BookingPaymentProcessor> logger)
    {
        _dbContext = dbContext;
        _stripe = stripe;
        _stripeConnect = stripeConnect;
        _logger = logger;
    }

    public async Task<BookingPaymentProcessingResult> ProcessAsync(
        long workloadId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _dbContext.Database.CurrentTransaction is null
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var payment = await _dbContext.BookingPayments
            .Include(item => item.Job)
            .ThenInclude(item => item.Business)
            .SingleOrDefaultAsync(item => item.WorkloadId == workloadId, cancellationToken);
        if (payment is null)
        {
            return new BookingPaymentProcessingResult(false, false, "Booking payment record not found.");
        }

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({payment.Job.BusinessId}, {7612})",
            cancellationToken);

        if (string.Equals(payment.PaymentStatus, BookingPaymentStatusNames.ChargeSucceeded, StringComparison.OrdinalIgnoreCase))
        {
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new BookingPaymentProcessingResult(true, true, null);
        }

        if (string.Equals(payment.PaymentStatus, BookingPaymentStatusNames.DistributionFailed, StringComparison.OrdinalIgnoreCase))
        {
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new BookingPaymentProcessingResult(false, false, "The Stripe transfer failed and requires payment-distribution review before this booking can be marked paid.");
        }

        if (payment.Job.StartDateTime <= DateTime.UtcNow)
        {
            MarkFailed(payment, "booking_expired", "The scheduled service start has passed.");
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new BookingPaymentProcessingResult(false, false, payment.LastPaymentFailureMessage);
        }

        if (string.IsNullOrWhiteSpace(payment.StripeCustomerId) || string.IsNullOrWhiteSpace(payment.StripePaymentMethodId))
        {
            MarkFailed(payment, "missing_payment_method", "No saved payment method is available.");
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new BookingPaymentProcessingResult(false, false, payment.LastPaymentFailureMessage);
        }

        var paymentAccount = await _dbContext.BusinessPaymentAccounts
            .SingleOrDefaultAsync(item => item.BusinessId == payment.Job.BusinessId, cancellationToken);
        if (paymentAccount is null || string.IsNullOrWhiteSpace(paymentAccount.StripeConnectedAccountId))
        {
            MarkFailed(payment, "business_payment_account_not_connected", "The business must connect Stripe before this booking can be charged.");
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new BookingPaymentProcessingResult(false, false, payment.LastPaymentFailureMessage);
        }

        StripeConnectAccountResult refreshedAccount;
        try
        {
            refreshedAccount = await _stripeConnect.GetAccountAsync(paymentAccount.StripeConnectedAccountId, cancellationToken);
        }
        catch (Exception ex) when (ex is StripeException or InvalidOperationException)
        {
            _logger.LogError(ex, "Could not verify Stripe readiness for business {BusinessId}.", payment.Job.BusinessId);
            MarkFailed(payment, "business_payment_account_unavailable", "Stripe could not verify the business payment account. Retry after checking the business Stripe setup.");
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new BookingPaymentProcessingResult(false, false, payment.LastPaymentFailureMessage);
        }

        paymentAccount.Status = refreshedAccount.Status;
        paymentAccount.TransfersCapabilityStatus = refreshedAccount.TransfersCapabilityStatus;
        paymentAccount.PayoutsCapabilityStatus = refreshedAccount.PayoutsCapabilityStatus;
        paymentAccount.UpdatedDateTime = DateTime.UtcNow;
        var readiness = StripeConnectPaymentReadiness.Evaluate(paymentAccount.StripeConnectedAccountId, refreshedAccount);
        if (!readiness.IsReady)
        {
            MarkFailed(payment, readiness.FailureCode, readiness.FailureMessage!);
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new BookingPaymentProcessingResult(false, false, payment.LastPaymentFailureMessage);
        }

        var allocation = BookingPaymentAllocation.Calculate(payment.Job.Cost, payment.Job.NetCost);

        try
        {
            var charge = await _stripe.ChargeSavedPaymentMethodAsync(
                new StripeOffSessionChargeRequest(
                    payment.StripeCustomerId,
                    payment.StripePaymentMethodId,
                    payment.Job.Cost,
                    "usd",
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["businessCode"] = payment.Job.Business.BusinessCode,
                        ["jobId"] = payment.JobId.ToString(CultureInfo.InvariantCulture),
                        ["workloadId"] = workloadId.ToString(CultureInfo.InvariantCulture),
                        ["chargeReason"] = "accepted_booking",
                        ["businessAmount"] = payment.Job.NetCost.ToString("0.00", CultureInfo.InvariantCulture),
                        ["platformFeeAmount"] = (payment.Job.Cost - payment.Job.NetCost).ToString("0.00", CultureInfo.InvariantCulture),
                    },
                    paymentAccount.StripeConnectedAccountId,
                    (decimal)allocation.PlatformFeeAmount / 100m),
                cancellationToken);

            payment.StripePaymentIntentId = charge.PaymentIntentId;
            payment.StripeConnectedAccountId = paymentAccount.StripeConnectedAccountId;
            payment.StripeChargeId = charge.ChargeId;
            payment.StripeTransferId = charge.TransferId;
            payment.StripeApplicationFeeId = charge.ApplicationFeeId;
            if (!string.Equals(charge.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                MarkFailed(payment, charge.Status, "The saved payment method requires additional customer action.");
                await _dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
                return new BookingPaymentProcessingResult(false, false, payment.LastPaymentFailureMessage);
            }

            if (string.IsNullOrWhiteSpace(charge.ChargeId) ||
                string.IsNullOrWhiteSpace(charge.TransferId) ||
                string.IsNullOrWhiteSpace(charge.ApplicationFeeId))
            {
                payment.PaymentStatus = BookingPaymentStatusNames.DistributionPending;
                payment.LastPaymentFailureUtc = DateTime.UtcNow;
                payment.LastPaymentFailureCode = "stripe_distribution_pending";
                payment.LastPaymentFailureMessage = "The customer payment succeeded, but Stripe distribution identifiers are still being reconciled.";
                payment.UpdatedDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
                return new BookingPaymentProcessingResult(false, false, payment.LastPaymentFailureMessage);
            }

            BookingPaymentStatusReconciler.MarkSucceeded(payment, DateTime.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new BookingPaymentProcessingResult(true, false, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Accepted-booking charge failed for workload {WorkloadId}.", workloadId);
            MarkFailed(payment, ex.StripeError?.Code, ex.StripeError?.Message ?? ex.Message);
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new BookingPaymentProcessingResult(false, false, payment.LastPaymentFailureMessage);
        }
    }

    private static void MarkFailed(BookingPayment payment, string? code, string message)
    {
        BookingPaymentStatusReconciler.MarkFailed(payment, DateTime.UtcNow, code, message);
    }
}
