using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Holonix.Server.Application.Services;
using Holonix.Server.Contracts.Business;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holonix.Server.Controllers;

[ApiController, Authorize, Route("api/business/{businessCode}/jobs")]
public sealed class BusinessJobsController(ApplicationDbContext db, JobTeamValidator validator) : ControllerBase
{
    private sealed record Access(int BusinessId, long MemberId, string UserId, bool Manage, bool Financial);
    private sealed class Row { public Job Job { get; set; } = null!; public string Status { get; set; } = ""; }
    private static readonly string[] Active = ["Scheduled", "Ready To Start", "In Progress"];
    private static DateTime WallClock(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    private IActionResult Error(string message, int status = 400) => StatusCode(status, new { errors = new[] { message } });

    private async Task<Access?> AuthorizeBusiness(string code, CancellationToken ct)
    {
        code = code.Trim().ToUpperInvariant();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var now = DateTime.UtcNow;
        var roles = await db.BusinessUserRoles.AsNoTracking().Where(r => r.BusinessUser.UserId == userId && r.BusinessUser.Business.BusinessCode == code
            && r.BusinessUser.Business.InactiveDate == null && r.BusinessUser.User.InactiveDate == null && r.BusinessUser.IsActive && r.BusinessUser.StartDate <= now
            && r.BusinessUser.EndDate == null && r.StartDate <= now && r.EndDate == null)
            .Select(r => new { r.BusinessUser.BusinessId, r.BusinessUserId, r.BusinessRole.Name }).ToListAsync(ct);
        if (roles.Count == 0) return null;
        var financial = roles.Any(r => r.Name == "Owner" || r.Name == "Administrator");
        return new(roles[0].BusinessId, roles[0].BusinessUserId, userId, financial || roles.Any(r => r.Name == "Manager"), financial);
    }

    private IQueryable<Row> Rows(Access access)
    {
        return db.Jobs.AsNoTracking().Include(j => j.User).Include(j => j.CompletedByUser)
            .Where(j => j.BusinessId == access.BusinessId && (!db.JobWorkloads.Any(w => w.JobId == j.JobId)
                || db.JobWorkloads.Any(w => w.JobId == j.JobId && (w.Workload.WorkloadType.Type == "Manual Job"
                    || (w.Workload.WorkloadType.Type == "Customer Booking" && new[] { "Accepted", "Scheduled", "Ready To Start", "In Progress", "Completed", "Cancelled" }.Contains(w.Workload.WorkloadStatus.Status))))))
            .Select(j => new Row { Job = j, Status = j.CompletedDateTime != null ? "Completed"
                : db.JobWorkloads.Any(w => w.JobId == j.JobId && w.Workload.WorkloadStatus.Status == "Cancelled") ? "Cancelled"
                : db.JobWorkloads.Any(w => w.JobId == j.JobId && w.Workload.WorkloadStatus.Status == "In Progress") ? "In Progress"
                : db.JobWorkloads.Any(w => w.JobId == j.JobId && w.Workload.WorkloadStatus.Status == "Ready To Start") ? "Ready To Start"
                : db.JobWorkloads.Any(w => w.JobId == j.JobId && w.Workload.WorkloadStatus.Status == "Completed") ? "Completed" : "Scheduled" });
    }

    [HttpGet]
    public async Task<IActionResult> List(string businessCode, [FromQuery] JobsQuery query, CancellationToken ct)
    {
        var access = await AuthorizeBusiness(businessCode, ct);
        if (access == null) return Error("You do not have access to this business.", 403);
        if (query.Start > query.End || !new[] { "all", "upcoming", "inprogress", "unassigned", "completed", "cancelled", "overdue" }.Contains(query.Filter)) return Error("Choose valid dates and a job filter.");
        var rows = Rows(access);
        if (query.Start.HasValue) { var from = WallClock(query.Start.Value.ToDateTime(TimeOnly.MinValue)); rows = rows.Where(r => r.Job.StartDateTime >= from); }
        if (query.End.HasValue) { if (query.End == DateOnly.MaxValue) return Error("Choose a valid end date."); var until = WallClock(query.End.Value.AddDays(1).ToDateTime(TimeOnly.MinValue)); rows = rows.Where(r => r.Job.StartDateTime < until); }
        if (!string.IsNullOrWhiteSpace(query.Search)) { var term = query.Search.Trim().ToLower(); rows = rows.Where(r => (r.Job.Name ?? "").ToLower().Contains(term) || (r.Job.User != null && (r.Job.User.FirstName + " " + r.Job.User.LastName).ToLower().Contains(term)) || db.JobAssignments.Any(a => a.JobId == r.Job.JobId && (a.BusinessUser.User.FirstName + " " + a.BusinessUser.User.LastName).ToLower().Contains(term)) || (r.Job.BusinessUser != null && (r.Job.BusinessUser.User.FirstName + " " + r.Job.BusinessUser.User.LastName).ToLower().Contains(term))); }
        if (query.EmployeeId.HasValue) { var id = query.EmployeeId.Value; rows = id == 0 ? rows.Where(r => r.Job.BusinessUserId == null && !db.JobAssignments.Any(a => a.JobId == r.Job.JobId)) : rows.Where(r => db.JobAssignments.Any(a => a.JobId == r.Job.JobId && a.BusinessUserId == id) || (!db.JobAssignments.Any(a => a.JobId == r.Job.JobId) && r.Job.BusinessUserId == id)); }
        var now = DateTime.UtcNow;
        var summary = await rows.GroupBy(r => 1).Select(g => new JobsSummary(g.Count(), g.Count(r => Active.Contains(r.Status) && r.Job.StartDateTime > now), g.Count(r => r.Status == "In Progress"),
            g.Count(r => Active.Contains(r.Status) && r.Job.BusinessUserId == null && !db.JobAssignments.Any(a => a.JobId == r.Job.JobId)), g.Count(r => r.Status == "Completed"), g.Count(r => r.Status == "Cancelled"),
            g.Count(r => Active.Contains(r.Status) && r.Job.EndDateTime < now), access.Financial ? g.Sum(r => r.Status != "Cancelled" ? r.Job.Cost : 0) : (decimal?)null)).SingleOrDefaultAsync(ct)
            ?? new JobsSummary(0, 0, 0, 0, 0, 0, 0, access.Financial ? 0 : null);
        rows = query.Filter switch {
            "upcoming" => rows.Where(r => Active.Contains(r.Status) && r.Job.StartDateTime > now),
            "inprogress" => rows.Where(r => r.Status == "In Progress"),
            "unassigned" => rows.Where(r => Active.Contains(r.Status) && r.Job.BusinessUserId == null && !db.JobAssignments.Any(a => a.JobId == r.Job.JobId)),
            "completed" => rows.Where(r => r.Status == "Completed"), "cancelled" => rows.Where(r => r.Status == "Cancelled"),
            "overdue" => rows.Where(r => Active.Contains(r.Status) && r.Job.EndDateTime < now), _ => rows };
        var count = await rows.CountAsync(ct);
        var page = await rows.OrderBy(r => Active.Contains(r.Status) ? 0 : 1).ThenBy(r => r.Job.StartDateTime).ThenBy(r => r.Job.JobId).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        var day = DateTime.UtcNow.Date;
        var todayQuery = Rows(access).Where(r => Active.Contains(r.Status) && r.Job.StartDateTime < day.AddDays(1) && r.Job.EndDateTime > day);
        var todayCount = await todayQuery.CountAsync(ct);
        var today = await todayQuery.OrderBy(r => r.Job.StartDateTime).ThenBy(r => r.Job.JobId).Take(100).ToListAsync(ct);
        var mapped = await Map(page.Concat(today).DistinctBy(r => r.Job.JobId).ToList(), access, ct);
        return Ok(new JobsPage(summary, page.Select(r => mapped[r.Job.JobId]).ToArray(), count, query.Page, query.PageSize, today.Select(r => mapped[r.Job.JobId]).ToArray(), todayCount, access.Manage, access.Financial, access.Financial, businessCode));
    }

    private async Task<Dictionary<long, OperationalJob>> Map(IReadOnlyList<Row> rows, Access access, CancellationToken ct)
    {
        var ids = rows.Select(r => r.Job.JobId).ToArray();
        var teams = await db.JobAssignments.AsNoTracking().Where(a => ids.Contains(a.JobId) && a.BusinessUser.BusinessId == access.BusinessId).Select(a => new { a.JobId, Member = new BusinessWorkspaceJobAssignmentResponse(a.BusinessUserId, a.BusinessUser.User.FirstName + " " + a.BusinessUser.User.LastName, a.BusinessUser.User.ProfileImageBase64) }).ToListAsync(ct);
        var legacyIds = rows.Where(r => r.Job.BusinessUserId.HasValue).Select(r => r.Job.BusinessUserId!.Value).ToArray();
        var legacy = await db.BusinessUsers.AsNoTracking().Where(u => legacyIds.Contains(u.BusinessUserId) && u.BusinessId == access.BusinessId).Select(u => new BusinessWorkspaceJobAssignmentResponse(u.BusinessUserId, u.User.FirstName + " " + u.User.LastName, u.User.ProfileImageBase64)).ToListAsync(ct);
        var lines = await db.JobServiceLines.AsNoTracking().Where(l => ids.Contains(l.JobId)).Select(l => new { l.JobId, l.ServiceName }).ToListAsync(ct);
        var payments = await db.BookingPayments.AsNoTracking().Where(p => ids.Contains(p.JobId)).Select(p => new { p.JobId, p.PaymentStatus }).ToDictionaryAsync(p => p.JobId, p => p.PaymentStatus, ct);
        return rows.ToDictionary(r => r.Job.JobId, r => {
            var j = r.Job;
            var members = teams.Where(t => t.JobId == j.JobId).Select(t => t.Member).ToList();
            if (members.Count == 0 && j.BusinessUserId.HasValue) members.AddRange(legacy.Where(u => u.BusinessUserId == j.BusinessUserId));
            var assigned = members.Any(m => m.BusinessUserId == access.MemberId);
            var active = Active.Contains(r.Status);
            var canWork = access.Manage || assigned;
            var payment = payments.GetValueOrDefault(j.JobId);
            var restriction = access.Manage && payment == "Charge Succeeded" ? "This booking has been charged. Cancellation requires a payment-resolution workflow; no refund is issued here." : null;
            var location = string.IsNullOrWhiteSpace(j.ServiceAddress1) ? null : new BusinessWorkspaceJobLocationResponse(j.ServiceLocationLabel, j.ServiceAddress1, j.ServiceAddress2, j.ServiceCity ?? "", j.ServiceState ?? "", j.ServiceZipCode ?? "", j.ServiceLatitude, j.ServiceLongitude);
            return new OperationalJob(j.JobId, j.Name ?? $"Job #{j.JobId}", j.User == null ? "Customer not linked" : (j.User.FirstName + " " + j.User.LastName).Trim(), j.User?.Email, j.User?.PhoneNumber,
                j.StartDateTime, j.EndDateTime, r.Status, access.Financial ? j.Cost : null, access.Financial ? payment ?? "Not recorded" : null, members, location,
                lines.Where(l => l.JobId == j.JobId).Select(l => l.ServiceName).ToArray(), j.CompletedByUser == null ? null : (j.CompletedByUser.FirstName + " " + j.CompletedByUser.LastName).Trim(), j.CompletedDateTime,
                Revision(j, r.Status, members.Select(m => m.BusinessUserId)), canWork && r.Status == "Scheduled" && members.Count > 0,
                canWork && r.Status == "Ready To Start" && members.Count > 0 && j.StartDateTime <= DateTime.UtcNow,
                canWork && r.Status == "In Progress", access.Manage && r.Status is "Scheduled" or "Ready To Start", access.Manage && r.Status is "Scheduled" or "Ready To Start", access.Manage && active && restriction == null, restriction);
        });
    }
    private static string Revision(Job job, string status, IEnumerable<long> team) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{job.JobId}|{job.StartDateTime:O}|{job.EndDateTime:O}|{job.CompletedDateTime:O}|{status}|{string.Join(',', team.Order())}")));

    [HttpGet("{jobId:long}")]
    public async Task<IActionResult> Detail(string businessCode, long jobId, CancellationToken ct)
    {
        var access = await AuthorizeBusiness(businessCode, ct); if (access == null) return Error("Access denied.", 403);
        var row = await Rows(access).SingleOrDefaultAsync(r => r.Job.JobId == jobId, ct); if (row == null) return Error("Operational job not found. Pending bookings are managed in Overview.", 404);
        return Ok((await Map([row], access, ct))[jobId]);
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(string businessCode, CancellationToken ct)
    {
        var access = await AuthorizeBusiness(businessCode, ct); if (access == null) return Error("Access denied.", 403);
        var employees = await db.BusinessUsers.AsNoTracking().Where(u => u.BusinessId == access.BusinessId).OrderBy(u => u.BusinessUserId).Take(501).Select(u => new JobEmployeeOption(u.BusinessUserId, u.User.FirstName + " " + u.User.LastName, u.User.ProfileImageBase64, "", true, false)).ToListAsync(ct);
        if (employees.Count > 500) return Error("Too many employees to display. Narrow your business roster.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var services = await db.BusinessSubServices.AsNoTracking().Where(s => s.BusinessId == access.BusinessId && s.EffectiveDate <= today && s.InactiveDate == null && !s.ConsultationNeeded).OrderBy(s => s.Name).Take(501).Select(s => new JobServiceOption(s.BusinessSubServiceId, s.Name, access.Financial ? s.Price : null, s.DurationMinutes, s.RequiresServiceLocation)).ToListAsync(ct);
        if (services.Count > 500) return Error("Too many services to display.");
        var customers = await db.Jobs.AsNoTracking().Where(j => j.BusinessId == access.BusinessId && j.UserId != null && j.User!.InactiveDate == null)
            .Select(j => new { UserId = j.UserId!, Name = j.User!.FirstName + " " + j.User.LastName }).Distinct().OrderBy(c => c.Name).Take(500).ToListAsync(ct);
        return Ok(new JobOptions(employees, services, customers.Select(c => new JobCustomerOption(c.UserId, c.Name)).ToArray()));
    }

    [HttpGet("{jobId:long}/team-options")]
    public async Task<IActionResult> TeamOptions(string businessCode, long jobId, CancellationToken ct)
    {
        var access = await AuthorizeBusiness(businessCode, ct); if (access == null || !access.Manage) return Error("Only managers, administrators, and owners can assign teams.", 403);
        var row = await Rows(access).SingleOrDefaultAsync(r => r.Job.JobId == jobId, ct); if (row == null || rNotEditable(row)) return Error("This job's team cannot be changed.");
        return Ok(await validator.Options(access.BusinessId, row.Job, ct));
    }
    private static bool rNotEditable(Row row) => row.Status is not ("Scheduled" or "Ready To Start");

    [HttpPost("{jobId:long}/ready")]
    public Task<IActionResult> Ready(string businessCode, long jobId, [FromBody] JobCommand command, CancellationToken ct) => Mutate(businessCode, jobId, "ready", command.Revision, null, null, ct);
    [HttpPost("{jobId:long}/start")]
    public Task<IActionResult> Start(string businessCode, long jobId, [FromBody] JobCommand command, CancellationToken ct) => Mutate(businessCode, jobId, "start", command.Revision, null, null, ct);
    [HttpPost("{jobId:long}/complete")]
    public Task<IActionResult> Complete(string businessCode, long jobId, [FromBody] JobCommand command, CancellationToken ct) => Mutate(businessCode, jobId, "complete", command.Revision, null, null, ct);
    [HttpPost("{jobId:long}/cancel")]
    public Task<IActionResult> Cancel(string businessCode, long jobId, [FromBody] JobCommand command, CancellationToken ct) => Mutate(businessCode, jobId, "cancel", command.Revision, null, null, ct);
    [HttpPost("{jobId:long}/reassign")]
    public Task<IActionResult> Reassign(string businessCode, long jobId, [FromBody] JobTeamCommand command, CancellationToken ct) => Mutate(businessCode, jobId, "team", command.Revision, command.BusinessUserIds, null, ct);
    [HttpPost("{jobId:long}/reschedule")]
    public Task<IActionResult> Reschedule(string businessCode, long jobId, [FromBody] JobScheduleCommand command, CancellationToken ct) => Mutate(businessCode, jobId, "schedule", command.Revision, null, command, ct);

    private async Task<IActionResult> Mutate(string code, long jobId, string action, string? revision, IReadOnlyList<long>? newTeam, JobScheduleCommand? schedule, CancellationToken ct)
    {
        var access = await AuthorizeBusiness(code, ct); if (access == null) return Error("Access denied.", 403);
        await using var transaction = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync(ct) : null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({access.BusinessId}, {7612})", ct);
        var row = await Rows(access).SingleOrDefaultAsync(r => r.Job.JobId == jobId, ct);
        if (row == null) return Error("Operational job not found. Pending bookings must be accepted first.", 404);
        var mapped = (await Map([row], access, ct))[jobId];
        if (revision != null && revision != mapped.Revision) return Error("This job changed. Refresh it before saving.", 409);
        if (!access.Manage && (action is "ready" or "start" or "complete") && !mapped.AssignedEmployees.Any(e => e.BusinessUserId == access.MemberId)) return Error("Only an assigned employee or manager can update work status.", 403);
        if (!access.Manage && (action is "team" or "schedule" or "cancel")) return Error("Only a manager can perform this action.", 403);
        var job = await db.Jobs.SingleAsync(j => j.JobId == jobId && j.BusinessId == access.BusinessId, ct);
        var team = mapped.AssignedEmployees.Select(e => e.BusinessUserId).ToArray();
        string next = row.Status;
        if (action == "team")
        {
            if (!mapped.CanReassign) return Error("Assignments are locked after work starts or ends.");
            var ids = newTeam ?? [];
            var error = await validator.Validate(access.BusinessId, job, ids, ct); if (error != null) return Error(error);
            var old = await db.JobAssignments.Where(a => a.JobId == jobId).ToListAsync(ct);
            db.JobAssignments.RemoveRange(old.Where(a => !ids.Contains(a.BusinessUserId)));
            db.JobAssignments.AddRange(ids.Where(id => old.All(a => a.BusinessUserId != id)).Select(id => new JobAssignment { JobId = jobId, BusinessUserId = id }));
            job.BusinessUserId = ids.Count > 0 ? ids[0] : null;
            next = "Scheduled";
        }
        else if (action == "schedule")
        {
            if (!mapped.CanReschedule || schedule == null) return Error("Only scheduled work can be rescheduled.");
            var from = WallClock(schedule.StartDateTime); var until = WallClock(schedule.EndDateTime);
            if (!ValidWindow(from, until)) return Error("Choose a future schedule lasting up to seven days.");
            job.StartDateTime = from; job.EndDateTime = until;
            var error = await validator.Validate(access.BusinessId, job, team, ct); if (error != null) return Error(error);
            next = "Scheduled";
        }
        else if (action == "cancel")
        {
            if (!mapped.CanCancel) return Error(mapped.CancellationRestriction ?? "This job cannot be cancelled.");
            next = "Cancelled";
        }
        else
        {
            var allowed = action == "ready" ? mapped.CanReady : action == "start" ? mapped.CanStart : mapped.CanComplete;
            if (!allowed) return Error("This action is not allowed for your role, job status, team, or scheduled start.", 409);
            if (action is "ready" or "start") { var error = await validator.Validate(access.BusinessId, job, team, ct); if (error != null) return Error(error); }
            next = action == "ready" ? "Ready To Start" : action == "start" ? "In Progress" : "Completed";
            if (next == "Completed") { job.CompletedByUserId = access.UserId; job.CompletedDateTime = DateTime.UtcNow; }
        }
        await SetStatus(job, next, access.UserId, ct);
        await db.SaveChangesAsync(ct); if (transaction != null) await transaction.CommitAsync(ct);
        return Ok(new JobRequestActionResponse(next, action == "team" ? "Team updated." : action == "schedule" ? "Job rescheduled." : "Job is now " + next.ToLowerInvariant() + "."));
    }
    private static bool ValidWindow(DateTime from, DateTime until) => from >= DateTime.UtcNow && until > from && until - from <= TimeSpan.FromDays(7);
    private async Task SetStatus(Job job, string status, string userId, CancellationToken ct)
    {
        var link = await db.JobWorkloads.Include(w => w.Workload).SingleOrDefaultAsync(w => w.JobId == job.JobId, ct);
        var type = link?.Workload.WorkloadTypeId ?? await db.WorkloadTypes.Where(t => t.Type == "Manual Job").Select(t => t.WorkloadTypeId).SingleAsync(ct);
        var target = await db.WorkloadStatuses.Where(s => s.WorkloadTypeId == type && s.Status == status).Select(s => s.WorkloadStatusId).SingleAsync(ct);
        if (link != null) link.Workload.WorkloadStatusId = target;
        else db.JobWorkloads.Add(new JobWorkload { Job = job, Workload = new Workload { WorkloadTypeId = type, WorkloadStatusId = target, CreatedByUserId = userId, CreatedDate = DateTime.UtcNow } });
    }

    [HttpPost]
    public async Task<IActionResult> Create(string businessCode, [FromBody] CreateManualJobRequest request, CancellationToken ct)
    {
        var access = await AuthorizeBusiness(businessCode, ct); if (access == null || !access.Financial) return Error("Only owners and administrators can create valued manual jobs.", 403);
        var from = WallClock(request.StartDateTime); var until = WallClock(request.EndDateTime);
        if (!ValidWindow(from, until) || string.IsNullOrWhiteSpace(request.Name)) return Error("Enter a name and a future schedule lasting up to seven days.");
        var ids = request.BusinessSubServiceIds.Distinct().ToArray();
        if (ids.Length == 0 || ids.Length > 50) return Error("Select between one and 50 services.");
        var services = await db.BusinessSubServices.Where(s => ids.Contains(s.BusinessSubServiceId) && s.BusinessId == access.BusinessId && s.EffectiveDate <= DateOnly.FromDateTime(from) && s.InactiveDate == null).ToListAsync(ct);
        if (services.Count != ids.Length || services.Any(s => s.ConsultationNeeded)) return Error("Select active fixed-price services. Consultation pricing must be agreed through the existing booking workflow.");
        if (services.Any(s => s.RequiresServiceLocation) && (request.ServiceLocation == null || string.IsNullOrWhiteSpace(request.ServiceLocation.Address1) || string.IsNullOrWhiteSpace(request.ServiceLocation.City) || string.IsNullOrWhiteSpace(request.ServiceLocation.State) || string.IsNullOrWhiteSpace(request.ServiceLocation.ZipCode))) return Error("Enter the service address, city, state and postal code for these services.");
        if (request.CustomerUserId != null && !await db.Jobs.AnyAsync(j => j.BusinessId == access.BusinessId && j.UserId == request.CustomerUserId && j.User!.InactiveDate == null, ct)) return Error("Select an existing customer of this business.");
        await using var transaction = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync(ct) : null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({access.BusinessId}, {7612})", ct);
        var job = new Job { BusinessId = access.BusinessId, Name = request.Name.Trim(), StartDateTime = from, EndDateTime = until, Cost = services.Sum(s => s.Price), NetCost = services.Sum(s => s.Price) };
        job.UserId = request.CustomerUserId;
        if (request.ServiceLocation is { } location)
        {
            job.ServiceAddress1 = location.Address1.Trim(); job.ServiceAddress2 = location.Address2?.Trim();
            job.ServiceCity = location.City.Trim(); job.ServiceState = location.State.Trim(); job.ServiceZipCode = location.ZipCode.Trim();
            job.ServiceLocationLabel = "Service address";
        }
        db.Jobs.Add(job);
        await SetStatus(job, "Scheduled", access.UserId, ct);
        db.JobServiceLines.AddRange(services.Select(s => new JobServiceLine { Job = job, BusinessSubServiceId = s.BusinessSubServiceId, ServiceName = s.Name, Price = s.Price, DurationMinutes = s.DurationMinutes }));
        await db.SaveChangesAsync(ct);
        var error = await validator.Validate(access.BusinessId, job, request.BusinessUserIds, ct); if (error != null) return Error(error);
        job.BusinessUserId = request.BusinessUserIds.FirstOrDefault() is var primary && primary > 0 ? primary : null;
        db.JobAssignments.AddRange(request.BusinessUserIds.Select(id => new JobAssignment { Job = job, BusinessUserId = id }));
        await db.SaveChangesAsync(ct); if (transaction != null) await transaction.CommitAsync(ct);
        return Ok(new { job.JobId, message = "Manual job created. No customer payment was collected." });
    }
}
