using System.Security.Claims;
using Holonix.Server.Contracts.Insights;
using Holonix.Server.Controllers;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Holonix.Server.Tests;

public class BusinessInsightsTests
{
    [Fact]
    public async Task Reports_AuthorizeScopeAndAggregateWithoutAssignmentDuplicates()
    {
        // All fixture writes are rolled back; existing business data is never modified.
        await using var db = new ApplicationDbContextFactory().CreateDbContext([]);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var owner = new ApplicationUser { Id = Guid.NewGuid().ToString(), FirstName = "=Insights", LastName = "Owner" };
        var employee = new ApplicationUser { Id = Guid.NewGuid().ToString(), FirstName = "Insights", LastName = "Employee" };
        var business = new Business { BusinessCode = Guid.NewGuid().ToString("N")[..10], StartDate = new DateOnly(2026, 1, 1), Name = "Reporting test" };
        var other = new Business { BusinessCode = Guid.NewGuid().ToString("N")[..10], StartDate = new DateOnly(2026, 1, 1), Name = "Other test" };
        db.AddRange(owner, employee, business, other);
        await db.SaveChangesAsync();
        var member = new BusinessUser { BusinessId = business.BusinessId, UserId = owner.Id, IsActive = true, StartDate = DateTime.UtcNow.AddDays(-1) };
        var staff = new BusinessUser { BusinessId = business.BusinessId, UserId = employee.Id, IsActive = true, StartDate = DateTime.UtcNow.AddDays(-1) };
        db.AddRange(member, staff); await db.SaveChangesAsync();
        var role = new BusinessUserRole { BusinessUserId = member.BusinessUserId, BusinessRoleId = await db.BusinessRoles.Where(r => r.Name == "Owner").Select(r => r.BusinessRoleId).SingleAsync(), StartDate = DateTime.UtcNow.AddDays(-1) };
        db.Add(role);
        var start = new DateTime(2026, 5, 10, 9, 0, 0, DateTimeKind.Utc);
        var completed = new Job { Name = "Completed fixture", BusinessId = business.BusinessId, StartDateTime = start, EndDateTime = start.AddHours(2), Cost = 200, NetCost = 190, CompletedDateTime = start.AddHours(2) };
        var scheduled = new Job { Name = "Scheduled fixture", BusinessId = business.BusinessId, StartDateTime = start.AddDays(1), EndDateTime = start.AddDays(1).AddHours(1), Cost = 100, NetCost = 95 };
        var cancelled = new Job { Name = "Cancelled fixture", BusinessId = business.BusinessId, StartDateTime = start, EndDateTime = start.AddHours(1), Cost = 800, NetCost = 760 };
        var foreign = new Job { BusinessId = other.BusinessId, StartDateTime = start, EndDateTime = start.AddHours(1), Cost = 9999 };
        db.AddRange(completed, scheduled, cancelled, foreign); await db.SaveChangesAsync();
        db.AddRange(new JobAssignment { JobId = completed.JobId, BusinessUserId = member.BusinessUserId }, new JobAssignment { JobId = completed.JobId, BusinessUserId = staff.BusinessUserId });
        var type = await db.WorkloadTypes.SingleAsync(t => t.Type == "Customer Booking");
        var status = await db.WorkloadStatuses.SingleAsync(s => s.WorkloadTypeId == type.WorkloadTypeId && s.Status == "Cancelled");
        var workload = new Workload { CreatedByUserId = owner.Id, CreatedDate = DateTime.UtcNow, WorkloadTypeId = type.WorkloadTypeId, WorkloadStatusId = status.WorkloadStatusId };
        db.Add(workload); await db.SaveChangesAsync();
        db.Add(new JobWorkload { JobId = cancelled.JobId, WorkloadId = workload.WorkloadId }); await db.SaveChangesAsync();
        var controller = new BusinessInsightsController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, owner.Id) }, "test")) } } };
        var query = new InsightsQuery { Start = new DateOnly(2026, 5, 1), End = new DateOnly(2026, 5, 31) };
        var result = Assert.IsType<OkObjectResult>(await controller.Get(business.BusinessCode, query, default));
        var report = Assert.IsType<InsightsResponse>(result.Value);
        Assert.Equal(3, report.TotalJobs);
        Assert.Equal(300, report.Summary.BookedValue);
        Assert.Equal(200, report.Summary.CompletedValue);
        Assert.Equal(150, report.Summary.AverageValue);
        Assert.Equal(1, report.Summary.CancelledJobs);
        Assert.Equal(2, report.Jobs.Single(j => j.JobId == completed.JobId).Employees.Length);
        Assert.Equal(300, Assert.Single(report.Groups).JobValue);
        Assert.Equal(300, report.Employees.Sum(e => e.BookedValue));
        Assert.Equal(200, report.Employees.Sum(e => e.CompletedValue));
        Assert.Equal(100, report.Employees.Single(e => e.EmployeeId == member.BusinessUserId).BookedValue);
        Assert.Equal(100, report.Employees.Single(e => e.EmployeeId == staff.BusinessUserId).BookedValue);
        Assert.Equal(100, report.Employees.Single(e => e.EmployeeId == 0).BookedValue);
        var csvResult = Assert.IsType<FileContentResult>(await controller.Export(business.BusinessCode, query, default));
        var csv = System.Text.Encoding.UTF8.GetString(csvResult.FileContents);
        Assert.Contains("Employee ID", csv);
        Assert.Contains("Equal share per assigned employee", csv);
        Assert.Contains("'\u003dInsights Owner", csv);
        query.GroupBy = "employee";
        var employeeGroups = Assert.IsType<InsightsResponse>(Assert.IsType<OkObjectResult>(await controller.Get(business.BusinessCode, query, default)).Value);
        Assert.Equal(300, employeeGroups.Groups.Sum(g => g.JobValue));
        Assert.Contains(employeeGroups.Groups, g => g.EmployeeId == staff.BusinessUserId);
        query.GroupBy = "month";
        foreach (var grouping in new[] { "day", "year", "status", "customer" })
        {
            query.GroupBy = grouping;
            var groupedResult = Assert.IsType<OkObjectResult>(await controller.Get(business.BusinessCode, query, default));
            Assert.Equal(300, Assert.IsType<InsightsResponse>(groupedResult.Value).Groups.Sum(g => g.JobValue));
        }
        query.GroupBy = "month";
        query.EmployeeId = staff.BusinessUserId;
        var employeeReport = Assert.IsType<InsightsResponse>(Assert.IsType<OkObjectResult>(await controller.Get(business.BusinessCode, query, default)).Value);
        Assert.Equal(1, employeeReport.TotalJobs);
        Assert.Equal(200, employeeReport.Summary.BookedValue);
        query.EmployeeId = null;
        query.Status = "Cancelled";
        var cancelledReport = Assert.IsType<InsightsResponse>(Assert.IsType<OkObjectResult>(await controller.Get(business.BusinessCode, query, default)).Value);
        Assert.Equal(0, cancelledReport.Summary.BookedValue);
        Assert.Equal(0, cancelledReport.Summary.AverageValue);
        query.Status = null;
        query.PageSize = 1;
        var pagedReport = Assert.IsType<InsightsResponse>(Assert.IsType<OkObjectResult>(await controller.Get(business.BusinessCode, query, default)).Value);
        Assert.Single(pagedReport.Jobs);
        Assert.Equal(3, pagedReport.TotalJobs);
        Assert.Equal(300, pagedReport.Summary.BookedValue);
        Assert.Equal(403, Assert.IsType<ObjectResult>(await controller.Get(other.BusinessCode, query, default)).StatusCode);
        // A legacy primary assignee must not create an extra share when the full team exists.
        completed.Cost = 200.01m;
        completed.BusinessUserId = member.BusinessUserId;
        staff.IsActive = false;
        await db.SaveChangesAsync();
        var cents = Assert.IsType<InsightsResponse>(Assert.IsType<OkObjectResult>(await controller.Get(business.BusinessCode, query, default)).Value);
        Assert.Equal(300.01m, cents.Employees.Sum(e => e.BookedValue));
        Assert.Equal(100.01m, cents.Employees.Single(e => e.EmployeeId == member.BusinessUserId).BookedValue);
        Assert.Equal(100m, cents.Employees.Single(e => e.EmployeeId == staff.BusinessUserId).BookedValue);
        query.EmployeeId = 0;
        var unassigned = Assert.IsType<InsightsResponse>(Assert.IsType<OkObjectResult>(await controller.Get(business.BusinessCode, query, default)).Value);
        Assert.Equal(100, unassigned.Summary.BookedValue);
        Assert.All(unassigned.Jobs, job => Assert.Empty(job.Employees));
        query.EmployeeId = null;
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, employee.Id) }, "test"));
        Assert.Equal(403, Assert.IsType<ObjectResult>(await controller.Get(business.BusinessCode, query, default)).StatusCode);
        Assert.Equal(403, Assert.IsType<ObjectResult>(await controller.Export(business.BusinessCode, query, default)).StatusCode);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, owner.Id) }, "test"));
        role.BusinessRoleId = await db.BusinessRoles.Where(r => r.Name == "Administrator").Select(r => r.BusinessRoleId).SingleAsync();
        await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>(await controller.Get(business.BusinessCode, query, default));
        member.IsActive = false; await db.SaveChangesAsync();
        Assert.Equal(403, Assert.IsType<ObjectResult>(await controller.Get(business.BusinessCode, query, default)).StatusCode);
        await transaction.RollbackAsync();
    }
}
