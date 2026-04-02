using Holonix.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holonix.Server.Infrastructure.Data;

public static class WorkloadReferenceSeeder
{
    private const string EmployeeInviteTypeName = WorkloadTypeNames.EmployeeInvite;
    private const string PendingInviteStatusName = WorkloadStatusNames.PendingInvite;
    private const string ActiveStatusName = WorkloadStatusNames.Active;
    private const string DeniedStatusName = WorkloadStatusNames.Denied;
    private const string ClosedStatusName = WorkloadStatusNames.Closed;

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

        var existingStatuses = await dbContext.WorkloadStatuses
            .AsNoTracking()
            .Where(workloadStatus => workloadStatus.WorkloadTypeId == employeeInviteType.WorkloadTypeId)
            .Select(workloadStatus => workloadStatus.Status)
            .ToListAsync(cancellationToken);

        if (existingStatuses.Contains(PendingInviteStatusName, StringComparer.OrdinalIgnoreCase) &&
            existingStatuses.Contains(ActiveStatusName, StringComparer.OrdinalIgnoreCase) &&
            existingStatuses.Contains(DeniedStatusName, StringComparer.OrdinalIgnoreCase) &&
            existingStatuses.Contains(ClosedStatusName, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (!existingStatuses.Contains(PendingInviteStatusName, StringComparer.OrdinalIgnoreCase))
        {
            dbContext.WorkloadStatuses.Add(new WorkloadStatus
            {
                WorkloadTypeId = employeeInviteType.WorkloadTypeId,
                Status = PendingInviteStatusName,
            });
        }

        if (!existingStatuses.Contains(ActiveStatusName, StringComparer.OrdinalIgnoreCase))
        {
            dbContext.WorkloadStatuses.Add(new WorkloadStatus
            {
                WorkloadTypeId = employeeInviteType.WorkloadTypeId,
                Status = ActiveStatusName,
            });
        }

        if (!existingStatuses.Contains(DeniedStatusName, StringComparer.OrdinalIgnoreCase))
        {
            dbContext.WorkloadStatuses.Add(new WorkloadStatus
            {
                WorkloadTypeId = employeeInviteType.WorkloadTypeId,
                Status = DeniedStatusName,
            });
        }

        if (!existingStatuses.Contains(ClosedStatusName, StringComparer.OrdinalIgnoreCase))
        {
            dbContext.WorkloadStatuses.Add(new WorkloadStatus
            {
                WorkloadTypeId = employeeInviteType.WorkloadTypeId,
                Status = ClosedStatusName,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
