using Holonix.Server.Contracts.Home;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Holonix.Server.Controllers;

[ApiController]
[Route("api/home")]
public class HomeController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public HomeController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Authorize]
    [HttpGet("pending-invites")]
    public async Task<IActionResult> GetPendingInvites(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(appUser => appUser.Id == userId && appUser.InactiveDate == null)
            .Select(appUser => new
            {
                appUser.Id,
                appUser.NormalizedEmail,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { errors = new[] { "User not found." } });
        }

        var invites = await _dbContext.EmployeeInviteWorkloads
            .AsNoTracking()
            .Where(item =>
                item.Workload.WorkloadType.Type == WorkloadTypeNames.EmployeeInvite &&
                item.Workload.WorkloadStatus.Status == WorkloadStatusNames.PendingInvite &&
                (
                    item.EmployeeInvite.TargetUserId == user.Id ||
                    (!string.IsNullOrWhiteSpace(user.NormalizedEmail) &&
                     item.EmployeeInvite.NormalizedTargetEmail == user.NormalizedEmail)
                ))
            .OrderByDescending(item => item.Workload.CreatedDate)
            .Select(item => new PendingInviteResponse(
                item.Workload.WorkloadId,
                item.Workload.WorkloadType.Type,
                item.Workload.CreatedDate,
                item.EmployeeInvite.Business.Name,
                item.EmployeeInvite.TargetBusinessRole != null ? item.EmployeeInvite.TargetBusinessRole.Name : null,
                BuildDisplayName(
                    item.Workload.CreatedByUser.FirstName,
                    item.Workload.CreatedByUser.LastName,
                    "Workspace User"),
                item.Workload.CreatedByUser.Email,
                item.Workload.WorkloadStatus.Status))
            .ToListAsync(cancellationToken);

        return Ok(invites);
    }

    [Authorize]
    [HttpPost("pending-invites/{workloadId:long}/accept")]
    public async Task<IActionResult> AcceptPendingInvite(long workloadId, CancellationToken cancellationToken)
    {
        var match = await GetPendingInviteMatchAsync(workloadId, cancellationToken);
        if (match.Result is not null)
        {
            return match.Result;
        }

        var pendingInvite = match.Value!;

        var activeStatus = await _dbContext.WorkloadStatuses
            .AsNoTracking()
            .Where(status =>
                status.WorkloadTypeId == pendingInvite.Workload.WorkloadTypeId &&
                status.Status == WorkloadStatusNames.Active)
            .Select(status => new { status.WorkloadStatusId })
            .SingleOrDefaultAsync(cancellationToken);

        if (activeStatus is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { errors = new[] { "Employee invite active status is unavailable." } });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var currentBusinessUser = await _dbContext.BusinessUsers
            .Where(businessUser =>
                businessUser.BusinessId == pendingInvite.EmployeeInvite.BusinessId &&
                businessUser.UserId == pendingInvite.UserId &&
                businessUser.IsActive &&
                businessUser.EndDate == null)
            .Select(businessUser => new { businessUser.BusinessUserId })
            .SingleOrDefaultAsync(cancellationToken);

        if (currentBusinessUser is null)
        {
            var businessUser = new BusinessUser
            {
                BusinessId = pendingInvite.EmployeeInvite.BusinessId,
                UserId = pendingInvite.UserId,
                IsActive = true,
                StartDate = DateTime.UtcNow,
            };

            _dbContext.BusinessUsers.Add(businessUser);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var roleId = pendingInvite.EmployeeInvite.TargetBusinessRoleId;
            if (!roleId.HasValue)
            {
                roleId = await _dbContext.BusinessRoles
                    .AsNoTracking()
                    .Where(role => role.Name == BusinessRoleNames.Employee)
                    .Select(role => (long?)role.BusinessRoleId)
                    .SingleOrDefaultAsync(cancellationToken);
            }

            if (!roleId.HasValue)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { errors = new[] { "Employee role is unavailable." } });
            }

            _dbContext.BusinessUserRoles.Add(new BusinessUserRole
            {
                BusinessUserId = businessUser.BusinessUserId,
                BusinessRoleId = roleId.Value,
                StartDate = DateTime.UtcNow,
            });
        }

        pendingInvite.Workload.WorkloadStatusId = activeStatus.WorkloadStatusId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("pending-invites/{workloadId:long}/deny")]
    public async Task<IActionResult> DenyPendingInvite(long workloadId, CancellationToken cancellationToken)
    {
        var match = await GetPendingInviteMatchAsync(workloadId, cancellationToken);
        if (match.Result is not null)
        {
            return match.Result;
        }

        var pendingInvite = match.Value!;

        var deniedStatus = await _dbContext.WorkloadStatuses
            .AsNoTracking()
            .Where(status =>
                status.WorkloadTypeId == pendingInvite.Workload.WorkloadTypeId &&
                status.Status == WorkloadStatusNames.Denied)
            .Select(status => new { status.WorkloadStatusId })
            .SingleOrDefaultAsync(cancellationToken);

        if (deniedStatus is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { errors = new[] { "Employee invite denied status is unavailable." } });
        }

        pendingInvite.Workload.WorkloadStatusId = deniedStatus.WorkloadStatusId;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string fallback)
    {
        var displayName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName) ? fallback : displayName;
    }

    private async Task<(PendingInviteMatch? Value, IActionResult? Result)> GetPendingInviteMatchAsync(long workloadId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return (null, Unauthorized(new { errors = new[] { "Invalid user context." } }));
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(appUser => appUser.Id == userId && appUser.InactiveDate == null)
            .Select(appUser => new
            {
                appUser.Id,
                appUser.NormalizedEmail,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return (null, Unauthorized(new { errors = new[] { "User not found." } }));
        }

        var pendingInvite = await _dbContext.EmployeeInviteWorkloads
            .Include(item => item.Workload)
            .Include(item => item.EmployeeInvite)
            .Where(item =>
                item.WorkloadId == workloadId &&
                item.Workload.WorkloadType.Type == WorkloadTypeNames.EmployeeInvite &&
                item.Workload.WorkloadStatus.Status == WorkloadStatusNames.PendingInvite &&
                (
                    item.EmployeeInvite.TargetUserId == user.Id ||
                    (!string.IsNullOrWhiteSpace(user.NormalizedEmail) &&
                     item.EmployeeInvite.NormalizedTargetEmail == user.NormalizedEmail)
                ))
            .SingleOrDefaultAsync(cancellationToken);

        if (pendingInvite is null)
        {
            return (null, NotFound(new { errors = new[] { "Pending invite not found." } }));
        }

        return (new PendingInviteMatch(user.Id, pendingInvite.Workload, pendingInvite.EmployeeInvite), null);
    }

    private sealed record PendingInviteMatch(string UserId, Workload Workload, EmployeeInvite EmployeeInvite);
}
