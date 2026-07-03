using Holonix.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holonix.Server.Infrastructure.Data;

public static class WorkloadReferenceSeeder
{
    private const string EmployeeInviteTypeName = WorkloadTypeNames.EmployeeInvite;
    private const string CustomerBookingTypeName = WorkloadTypeNames.CustomerBooking;
    private const string PendingInviteStatusName = WorkloadStatusNames.PendingInvite;
    private const string ActiveStatusName = WorkloadStatusNames.Active;
    private const string DeniedStatusName = WorkloadStatusNames.Denied;
    private const string ClosedStatusName = WorkloadStatusNames.Closed;
    private static readonly string[] CustomerBookingStatuses =
    [
        WorkloadStatusNames.PendingCheckout,
        WorkloadStatusNames.AwaitingAcceptance,
        WorkloadStatusNames.Accepted,
        WorkloadStatusNames.Scheduled,
        WorkloadStatusNames.ReadyToStart,
        WorkloadStatusNames.InProgress,
        WorkloadStatusNames.Completed,
        WorkloadStatusNames.CheckoutExpired,
        WorkloadStatusNames.PaymentFailed,
        WorkloadStatusNames.Cancelled,
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var employeeInviteType = await EnsureWorkloadTypeAsync(dbContext, EmployeeInviteTypeName, cancellationToken);
        await EnsureStatusesAsync(
            dbContext,
            employeeInviteType.WorkloadTypeId,
            [
                PendingInviteStatusName,
                ActiveStatusName,
                DeniedStatusName,
                ClosedStatusName,
            ],
            cancellationToken);

        var customerBookingType = await EnsureWorkloadTypeAsync(dbContext, CustomerBookingTypeName, cancellationToken);
        await EnsureStatusesAsync(
            dbContext,
            customerBookingType.WorkloadTypeId,
            CustomerBookingStatuses,
            cancellationToken);
    }

    private static async Task<WorkloadType> EnsureWorkloadTypeAsync(
        ApplicationDbContext dbContext,
        string typeName,
        CancellationToken cancellationToken)
    {
        var workloadType = await dbContext.WorkloadTypes
            .SingleOrDefaultAsync(existingType => existingType.Type == typeName, cancellationToken);

        if (workloadType is not null)
        {
            return workloadType;
        }

        workloadType = new WorkloadType
        {
            Type = typeName,
        };

        dbContext.WorkloadTypes.Add(workloadType);
        await dbContext.SaveChangesAsync(cancellationToken);
        return workloadType;
    }

    private static async Task EnsureStatusesAsync(
        ApplicationDbContext dbContext,
        long workloadTypeId,
        IReadOnlyCollection<string> statusNames,
        CancellationToken cancellationToken)
    {
        var existingStatuses = await dbContext.WorkloadStatuses
            .AsNoTracking()
            .Where(workloadStatus => workloadStatus.WorkloadTypeId == workloadTypeId)
            .Select(workloadStatus => workloadStatus.Status)
            .ToListAsync(cancellationToken);

        if (statusNames.All(statusName => existingStatuses.Contains(statusName, StringComparer.OrdinalIgnoreCase)))
        {
            return;
        }

        foreach (var statusName in statusNames)
        {
            if (existingStatuses.Contains(statusName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            dbContext.WorkloadStatuses.Add(new WorkloadStatus
            {
                WorkloadTypeId = workloadTypeId,
                Status = statusName,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
