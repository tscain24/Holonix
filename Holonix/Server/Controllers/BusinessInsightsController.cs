using System.Globalization;
using System.Security.Claims;
using System.Text;
using Holonix.Server.Contracts.Insights;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holonix.Server.Controllers;

[ApiController, Authorize, Route("api/business/{businessCode}/insights")]
public class BusinessInsightsController(ApplicationDbContext db) : ControllerBase
{
    private sealed class ReportRow
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public string Customer { get; set; } = "";
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public DateTime? CompletedAt { get; set; }
        public decimal Value { get; set; }
        public string Status { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
    }

    [HttpGet]
    public async Task<IActionResult> Get(string businessCode, [FromQuery] InsightsQuery query, CancellationToken ct)
    {
        var businessId = await AuthorizedBusiness(businessCode, ct);
        if (businessId == null) return StatusCode(403, new { errors = new[] { "Financial Insights requires an active Owner or Administrator role." } });
        if (query.End == DateOnly.MaxValue || query.End < query.Start || query.End.DayNumber - query.Start.DayNumber > 366 || query.MinValue < 0 || query.MaxValue < query.MinValue
            || !new[] { "day", "month", "year", "status", "customer", "employee" }.Contains(query.GroupBy))
            return BadRequest(new { errors = new[] { "Choose a valid date range of up to 367 days, value range, and grouping." } });

        var rows = Rows(businessId.Value, query);
        var now = DateTime.UtcNow;
        var summary = await rows.GroupBy(r => 1).Select(g => new InsightSummary(
            g.Count(), g.Sum(r => r.Status != "Cancelled" ? r.Value : 0),
            g.Sum(r => r.CompletedAt != null && r.Status != "Cancelled" ? r.Value : 0),
            g.Where(r => r.Status != "Cancelled").Average(r => (decimal?)r.Value) ?? 0,
            g.Count(r => r.CompletedAt != null && r.Status != "Cancelled"), g.Count(r => r.Status == "Cancelled"),
            g.Sum(r => r.CompletedAt == null && r.Status != "Cancelled" && r.Start > now ? r.Value : 0))).SingleOrDefaultAsync(ct)
            ?? new InsightSummary(0, 0, 0, 0, 0, 0, 0);

        var grouped = query.GroupBy switch
        {
            "day" => rows.GroupBy(r => new { Year = r.Start.Year, Month = r.Start.Month, Day = r.Start.Day, Label = "" }),
            "year" => rows.GroupBy(r => new { Year = r.Start.Year, Month = 0, Day = 0, Label = "" }),
            "status" => rows.GroupBy(r => new { Year = 0, Month = 0, Day = 0, Label = r.Status }),
            "customer" => rows.GroupBy(r => new { Year = 0, Month = 0, Day = 0, Label = r.Customer }),
            _ => rows.GroupBy(r => new { Year = r.Start.Year, Month = r.Start.Month, Day = 0, Label = "" })
        };
        var aggregates = await grouped.Select(g => new {
            g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Label,
            Jobs = g.Count(), Value = g.Sum(r => r.Status != "Cancelled" ? r.Value : 0),
            Average = g.Where(r => r.Status != "Cancelled").Average(r => (decimal?)r.Value) ?? 0,
            Completed = g.Count(r => r.CompletedAt != null && r.Status != "Cancelled"),
            CompletedValue = g.Sum(r => r.CompletedAt != null && r.Status != "Cancelled" ? r.Value : 0),
            Cancelled = g.Count(r => r.Status == "Cancelled")
        }).OrderBy(g => g.Year).ThenBy(g => g.Month).ThenBy(g => g.Day).ThenBy(g => g.Label).Take(501).ToListAsync(ct);
        if (aggregates.Count > 500) return BadRequest(new { errors = new[] { "Too many groups. Narrow the date range or filters." } });
        var groups = aggregates.Select(g => new InsightGroup(g.Label != "" ? g.Label : g.Day > 0 ? $"{g.Year:D4}-{g.Month:D2}-{g.Day:D2}" : g.Month > 0 ? $"{g.Year:D4}-{g.Month:D2}" : $"{g.Year:D4}", g.Jobs, g.Value, g.Average, g.Completed, g.CompletedValue, g.Cancelled)).ToList();
        var employees = await EmployeeBreakdown(rows, businessId.Value, ct);
        if (employees.Count > 500) return BadRequest(new { errors = new[] { "Too many employee groups. Narrow the report filters." } });
        if (query.GroupBy == "employee") groups = employees.Select(e => new InsightGroup(e.Name, e.Jobs, e.BookedValue,
            e.Jobs > e.CancelledJobs ? e.BookedValue / (e.Jobs - e.CancelledJobs) : 0, e.CompletedJobs, e.CompletedValue, e.CancelledJobs, e.EmployeeId)).ToList();
        var page = await rows.OrderByDescending(r => r.Start).ThenBy(r => r.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        var ids = page.Select(r => r.Id).ToArray();
        var assignments = await db.JobAssignments.AsNoTracking().Where(a => ids.Contains(a.JobId) && a.BusinessUser.BusinessId == businessId)
            .Select(a => new { a.JobId, Name = a.BusinessUser.User.FirstName + " " + a.BusinessUser.User.LastName }).ToListAsync(ct);
        var legacy = await db.Jobs.AsNoTracking().Where(j => ids.Contains(j.JobId) && j.BusinessId == businessId && j.BusinessUser != null && j.BusinessUser.BusinessId == businessId)
            .Select(j => new { j.JobId, Name = j.BusinessUser!.User.FirstName + " " + j.BusinessUser.User.LastName }).ToListAsync(ct);
        var services = await db.JobServiceLines.AsNoTracking().Where(l => ids.Contains(l.JobId) && l.Job.BusinessId == businessId).Select(l => new { l.JobId, l.ServiceName }).ToListAsync(ct);
        var jobs = page.Select(r => new InsightJob(r.Id, r.Name, r.Customer, r.Start, r.End, r.CompletedAt, r.Status, r.Value, r.PaymentStatus,
            assignments.Where(a => a.JobId == r.Id).Select(a => a.Name).Concat(legacy.Where(a => a.JobId == r.Id && !assignments.Any(x => x.JobId == r.Id)).Select(a => a.Name)).Distinct().ToArray(),
            services.Where(l => l.JobId == r.Id).Select(l => l.ServiceName).ToArray())).ToList();
        return Ok(new InsightsResponse(summary, groups, jobs, summary.Jobs, query.Page, query.PageSize, employees));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(string businessCode, [FromQuery] InsightsQuery query, CancellationToken ct)
    {
        // Reuse the authorized, validated and bounded report query. Export aggregates, never a truncated job page.
        var result = await Get(businessCode, query, ct);
        if (result is not OkObjectResult { Value: InsightsResponse report }) return result;
        static string Cell(string value) => "\"" + (value.Length > 0 && "=+-@\t\r\n".Contains(value[0]) ? "'" : "") + value.Replace("\"", "\"\"") + "\"";
        var csv = new StringBuilder("Breakdown,Employee ID,Group,Jobs,Booked job value,Average booked value,Completed jobs,Completed value,Cancelled jobs,Allocation\r\n");
        foreach (var g in report.Groups)
            csv.AppendLine(string.Join(",", query.GroupBy == "employee" ? "Employee" : "Report", g.EmployeeId?.ToString(CultureInfo.InvariantCulture) ?? "", Cell(g.Key), g.Jobs, g.JobValue.ToString(CultureInfo.InvariantCulture), g.AverageValue.ToString(CultureInfo.InvariantCulture), g.CompletedJobs, g.CompletedValue.ToString(CultureInfo.InvariantCulture), g.CancelledJobs, Cell(query.GroupBy == "employee" ? "Equal share per assigned employee; unassigned value retained separately" : "Whole-job value")));
        if (query.GroupBy != "employee")
            foreach (var e in report.Employees)
                csv.AppendLine(string.Join(",", "Employee", e.EmployeeId, Cell(e.Name), e.Jobs, e.BookedValue.ToString(CultureInfo.InvariantCulture), (e.Jobs > e.CancelledJobs ? e.BookedValue / (e.Jobs - e.CancelledJobs) : 0).ToString(CultureInfo.InvariantCulture), e.CompletedJobs, e.CompletedValue.ToString(CultureInfo.InvariantCulture), e.CancelledJobs, Cell("Equal share per assigned employee; unassigned value retained separately")));
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "holonix-report.csv");
    }

    private async Task<List<InsightEmployee>> EmployeeBreakdown(IQueryable<ReportRow> rows, int businessId, CancellationToken ct)
    {
        // Use current team assignments, falling back to the legacy assignee only when there is no team.
        var assignments = db.JobAssignments.AsNoTracking()
            .Where(a => a.Job.BusinessId == businessId && a.BusinessUser.BusinessId == businessId)
            .Select(a => new { a.JobId, EmployeeId = a.BusinessUserId }).Distinct();
        var legacy = db.Jobs.AsNoTracking().Where(j => j.BusinessId == businessId && j.BusinessUserId != null
            && j.BusinessUser!.BusinessId == businessId && !assignments.Any(a => a.JobId == j.JobId))
            .Select(j => new { j.JobId, EmployeeId = j.BusinessUserId!.Value });
        var teams = assignments.Union(legacy);
        var shares = from row in rows
                     join team in teams on row.Id equals team.JobId into joined
                     from team in joined.DefaultIfEmpty()
                     select new {
                         EmployeeId = (long?)team.EmployeeId,
                         row.Status, row.CompletedAt,
                         // Allocate cents deterministically so the employee rows reconcile exactly to job totals.
                         Value = !teams.Any(t => t.JobId == row.Id) ? row.Value
                             : Math.Floor(row.Value * 100 / teams.Count(t => t.JobId == row.Id)) / 100
                               + (teams.Count(t => t.JobId == row.Id && t.EmployeeId < team.EmployeeId) < row.Value * 100 % teams.Count(t => t.JobId == row.Id) ? 0.01m : 0m)
                     };
        var totals = await shares.GroupBy(s => s.EmployeeId).Select(g => new {
            Id = g.Key, Jobs = g.Count(),
            Value = g.Sum(s => s.Status != "Cancelled" ? s.Value : 0),
            CompletedValue = g.Sum(s => s.Status != "Cancelled" && s.CompletedAt != null ? s.Value : 0),
            Completed = g.Count(s => s.Status != "Cancelled" && s.CompletedAt != null),
            Cancelled = g.Count(s => s.Status == "Cancelled")
        }).OrderBy(g => g.Id).Take(501).ToListAsync(ct);
        var ids = totals.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToArray();
        var names = await db.BusinessUsers.AsNoTracking().Where(u => u.BusinessId == businessId && ids.Contains(u.BusinessUserId))
            .Select(u => new { u.BusinessUserId, Name = u.User.FirstName + " " + u.User.LastName }).ToDictionaryAsync(u => u.BusinessUserId, u => u.Name, ct);
        return totals.Select(t => new InsightEmployee(t.Id ?? 0, t.Id == null ? "Unassigned" : names.GetValueOrDefault(t.Id.Value, "Employee #" + t.Id),
            t.Jobs, t.Value, t.CompletedValue, t.Completed, t.Cancelled)).ToList();
    }

    private async Task<int?> AuthorizedBusiness(string code, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var now = DateTime.UtcNow;
        return await db.BusinessUserRoles.AsNoTracking().Where(r => r.BusinessUser.UserId == userId
            && r.BusinessUser.User.InactiveDate == null && r.BusinessUser.IsActive && r.BusinessUser.EndDate == null && r.BusinessUser.StartDate <= now
            && r.StartDate <= now && r.EndDate == null && r.BusinessUser.Business.InactiveDate == null
            && r.BusinessUser.Business.BusinessCode == code
            && (r.BusinessRole.Name == BusinessRoleNames.Owner || r.BusinessRole.Name == BusinessRoleNames.Administrator))
            .Select(r => (int?)r.BusinessUser.BusinessId).FirstOrDefaultAsync(ct);
    }

    private IQueryable<ReportRow> Rows(int businessId, InsightsQuery q)
    {
        // Business schedules are wall-clock timestamps, consistent with the Jobs workspace.
        var start = DateTime.SpecifyKind(q.Start.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(q.End.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var jobs = db.Jobs.AsNoTracking().Where(j => j.BusinessId == businessId && j.StartDateTime >= start && j.StartDateTime < end);
        // Pending requests are not booked revenue. Completed workload means accepted booking; job completion is tracked separately.
        jobs = jobs.Where(j => !db.JobWorkloads.Any(w => w.JobId == j.JobId)
            || db.JobWorkloads.Any(w => w.JobId == j.JobId && (w.Workload.WorkloadType.Type == WorkloadTypeNames.CustomerBooking || w.Workload.WorkloadType.Type == "Manual Job")
                && (w.Workload.WorkloadStatus.Status == "Completed" || w.Workload.WorkloadStatus.Status == "Accepted" || w.Workload.WorkloadStatus.Status == "Scheduled" || w.Workload.WorkloadStatus.Status == "In Progress" || w.Workload.WorkloadStatus.Status == "Ready To Start" || w.Workload.WorkloadStatus.Status == "Cancelled")));
        if (q.EmployeeId == 0) jobs = jobs.Where(j => !db.JobAssignments.Any(a => a.JobId == j.JobId && a.BusinessUser.BusinessId == businessId) && j.BusinessUserId == null);
        else if (q.EmployeeId.HasValue) jobs = jobs.Where(j => db.JobAssignments.Any(a => a.JobId == j.JobId && a.BusinessUserId == q.EmployeeId && a.BusinessUser.BusinessId == businessId)
            || (!db.JobAssignments.Any(a => a.JobId == j.JobId) && j.BusinessUserId == q.EmployeeId && j.BusinessUser!.BusinessId == businessId));
        if (!string.IsNullOrWhiteSpace(q.Service)) jobs = jobs.Where(j => db.JobServiceLines.Any(l => l.JobId == j.JobId && l.ServiceName.ToLower().Contains(q.Service.ToLower())));
        if (q.MinValue.HasValue) jobs = jobs.Where(j => j.Cost >= q.MinValue);
        if (q.MaxValue.HasValue) jobs = jobs.Where(j => j.Cost <= q.MaxValue);
        var rows = jobs.Select(j => new ReportRow {
            Id = j.JobId, Name = j.Name ?? "Untitled job", Customer = j.User != null ? j.User.FirstName + " " + j.User.LastName : "Unknown customer",
            Start = j.StartDateTime, End = j.EndDateTime, CompletedAt = j.CompletedDateTime, Value = j.Cost,
            Status = db.JobWorkloads.Any(w => w.JobId == j.JobId && w.Workload.WorkloadStatus.Status == "Cancelled") ? "Cancelled" : j.CompletedDateTime != null ? "Completed" : "Scheduled",
            PaymentStatus = db.BookingPayments.Where(p => p.JobId == j.JobId).OrderByDescending(p => p.BookingPaymentId).Select(p => p.PaymentStatus).FirstOrDefault() ?? "Not recorded"
        });
        if (!string.IsNullOrWhiteSpace(q.Status)) rows = rows.Where(r => r.Status == q.Status);
        if (!string.IsNullOrWhiteSpace(q.Customer)) rows = q.ExactCustomer ? rows.Where(r => r.Customer == q.Customer) : rows.Where(r => r.Customer.ToLower().Contains(q.Customer.ToLower()));
        return rows;
    }
}
