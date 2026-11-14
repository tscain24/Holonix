using Holonix.Server.Application.Interfaces;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Holonix.Server.Infrastructure.Services;

public sealed class ScheduledBookingPaymentWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledBookingPaymentWorker> _logger;

    public ScheduledBookingPaymentWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledBookingPaymentWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await ProcessDuePaymentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled booking payment scan failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessDuePaymentsAsync(CancellationToken cancellationToken)
    {
        await using var readScope = _scopeFactory.CreateAsyncScope();
        var db = readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dueWorkloadIds = await db.BookingPayments
            .AsNoTracking()
            .Where(item =>
                item.PaymentStatus == BookingPaymentStatusNames.ChargePending &&
                item.ChargeScheduledForUtc != null &&
                item.ChargeScheduledForUtc <= DateTime.UtcNow &&
                (item.Workload.WorkloadStatus.Status == WorkloadStatusNames.Accepted ||
                 item.Workload.WorkloadStatus.Status == WorkloadStatusNames.Scheduled))
            .OrderBy(item => item.ChargeScheduledForUtc)
            .Select(item => item.WorkloadId)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var workloadId in dueWorkloadIds)
        {
            await using var paymentScope = _scopeFactory.CreateAsyncScope();
            var processor = paymentScope.ServiceProvider.GetRequiredService<IBookingPaymentProcessor>();
            await processor.ProcessAsync(workloadId, cancellationToken);
        }
    }
}
