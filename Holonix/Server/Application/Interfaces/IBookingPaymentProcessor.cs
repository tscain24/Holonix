namespace Holonix.Server.Application.Interfaces;

public interface IBookingPaymentProcessor
{
    Task<BookingPaymentProcessingResult> ProcessAsync(long workloadId, CancellationToken cancellationToken = default);
}

public sealed record BookingPaymentProcessingResult(bool Succeeded, bool AlreadySucceeded, string? FailureMessage);
