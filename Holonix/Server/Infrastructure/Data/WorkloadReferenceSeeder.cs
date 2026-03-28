using Holonix.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holonix.Server.Infrastructure.Data;

public static class WorkloadReferenceSeeder
{
    private const string EmployeeInviteTypeName = WorkloadTypeNames.EmployeeInvite;
    private const string PendingInviteStatusName = WorkloadStatusNames.PendingInvite;

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var employeeInviteType = await dbContext.WorkloadTypes
            .SingleOrDefaultAsync(
                workloadType => workloadType.Type == EmployeeInviteTypeName,
                cancellationToken);

        if (employeeInviteType is null)
        {
            employeeInviteType = new WorkloadType
            {
                Type = EmployeeInviteTypeName,
            };

            dbContext.WorkloadTypes.Add(employeeInviteType);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var hasPendingInviteStatus = await dbContext.WorkloadStatuses
            .AnyAsync(
                workloadStatus =>
                    workloadStatus.WorkloadTypeId == employeeInviteType.WorkloadTypeId &&
                    workloadStatus.Status == PendingInviteStatusName,
                cancellationToken);

        if (hasPendingInviteStatus)
        {
            return;
        }

        dbContext.WorkloadStatuses.Add(new WorkloadStatus
        {
            WorkloadTypeId = employeeInviteType.WorkloadTypeId,
            Status = PendingInviteStatusName,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
