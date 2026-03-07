using Microsoft.EntityFrameworkCore;
using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Infrastructure.Data;

public static class BusinessRoleSeeder
{
    private const string OwnerRoleName = "Owner";
    private const long OwnerHierarchyNumber = 1000;

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var ownerRoleExists = await dbContext.BusinessRoles
            .AsNoTracking()
            .AnyAsync(role => role.Name == OwnerRoleName, cancellationToken);

        if (ownerRoleExists)
        {
            return;
        }

        dbContext.BusinessRoles.Add(new BusinessRole
        {
            Name = OwnerRoleName,
            HierarchyNumber = OwnerHierarchyNumber,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
