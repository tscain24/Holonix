using Microsoft.EntityFrameworkCore;
using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Infrastructure.Data;

public static class BusinessRoleSeeder
{
    private const string OwnerRoleName = BusinessRoleNames.Owner;
    private const string AdministratorRoleName = BusinessRoleNames.Administrator;
    private const string ManagerRoleName = BusinessRoleNames.Manager;
    private const string EmployeeRoleName = BusinessRoleNames.Employee;
    private const long OwnerHierarchyNumber = 1000;
    private const long AdministratorHierarchyNumber = 800;
    private const long ManagerHierarchyNumber = 700;
    private const long EmployeeHierarchyNumber = 500;

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existingRoleNames = await dbContext.BusinessRoles
            .AsNoTracking()
            .Select(role => role.Name)
            .ToListAsync(cancellationToken);

        if (existingRoleNames.Contains(OwnerRoleName, StringComparer.OrdinalIgnoreCase) &&
            existingRoleNames.Contains(AdministratorRoleName, StringComparer.OrdinalIgnoreCase) &&
            existingRoleNames.Contains(ManagerRoleName, StringComparer.OrdinalIgnoreCase) &&
            existingRoleNames.Contains(EmployeeRoleName, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (!existingRoleNames.Contains(OwnerRoleName, StringComparer.OrdinalIgnoreCase))
        {
            dbContext.BusinessRoles.Add(new BusinessRole
            {
                Name = OwnerRoleName,
                HierarchyNumber = OwnerHierarchyNumber,
            });
        }

        if (!existingRoleNames.Contains(AdministratorRoleName, StringComparer.OrdinalIgnoreCase))
        {
            dbContext.BusinessRoles.Add(new BusinessRole
            {
                Name = AdministratorRoleName,
                HierarchyNumber = AdministratorHierarchyNumber,
            });
        }

        if (!existingRoleNames.Contains(ManagerRoleName, StringComparer.OrdinalIgnoreCase))
        {
            dbContext.BusinessRoles.Add(new BusinessRole
            {
                Name = ManagerRoleName,
                HierarchyNumber = ManagerHierarchyNumber,
            });
        }

        if (!existingRoleNames.Contains(EmployeeRoleName, StringComparer.OrdinalIgnoreCase))
        {
            dbContext.BusinessRoles.Add(new BusinessRole
            {
                Name = EmployeeRoleName,
                HierarchyNumber = EmployeeHierarchyNumber,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
