using Holonix.Server.Application.Interfaces;
using Holonix.Server.Contracts.Business;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Holonix.Server.Application.Services;

public sealed class JobTeamValidator(ApplicationDbContext db, IBusinessAvailabilityCalculator calculator)
{
    public async Task<IReadOnlyList<JobEmployeeOption>> Options(int businessId, Job job, CancellationToken ct)
    {
        var current = await db.JobAssignments.AsNoTracking().Where(a => a.JobId == job.JobId).Select(a => a.BusinessUserId).ToListAsync(ct);
        if (current.Count == 0 && job.BusinessUserId.HasValue) current.Add(job.BusinessUserId.Value);
        var members = await db.BusinessUsers.AsNoTracking().Where(u => u.BusinessId == businessId && (current.Contains(u.BusinessUserId) || (u.IsActive && u.EndDate == null && u.StartDate <= DateTime.UtcNow && u.User.InactiveDate == null))).OrderBy(u => u.BusinessUserId).Take(501).Include(u => u.User).ToListAsync(ct);
        if (members.Count > 500) throw new InvalidOperationException("Assignment supports up to 500 employees.");
        var ids = members.Select(u => u.BusinessUserId).ToArray();
        var first = DateOnly.FromDateTime(job.StartDateTime);
        var last = DateOnly.FromDateTime(job.EndDateTime.AddTicks(-1));
        var hours = await db.BusinessUserAvailabilities.AsNoTracking().Where(a => ids.Contains(a.BusinessUserId) && a.EffectiveStartDate <= last && (a.EffectiveEndDate == null || a.EffectiveEndDate >= first)).ToListAsync(ct);
        var timeOff = await db.BusinessUserTimeOffs.AsNoTracking().Where(a => ids.Contains(a.BusinessUserId) && a.EffectiveStartDate <= last && a.EffectiveEndDate >= first).ToListAsync(ct);
        var conflicts = db.Jobs.AsNoTracking().Where(j => j.BusinessId == businessId && j.JobId != job.JobId && j.StartDateTime < job.EndDateTime && j.EndDateTime > job.StartDateTime && j.CompletedDateTime == null && (!db.JobWorkloads.Any(w => w.JobId == j.JobId) || db.JobWorkloads.Any(w => w.JobId == j.JobId && new[] { "Accepted", "Scheduled", "Ready To Start", "In Progress" }.Contains(w.Workload.WorkloadStatus.Status))));
        var busy = await db.JobAssignments.AsNoTracking().Where(a => conflicts.Any(j => j.JobId == a.JobId)).Select(a => a.BusinessUserId).ToListAsync(ct);
        busy.AddRange(await conflicts.Where(j => j.BusinessUserId != null && !db.JobAssignments.Any(a => a.JobId == j.JobId)).Select(j => j.BusinessUserId!.Value).ToListAsync(ct));
        var services = await db.JobServiceLines.AsNoTracking().Where(l => l.JobId == job.JobId && l.BusinessSubServiceId != null).Select(l => l.BusinessSubServiceId!.Value).Distinct().ToListAsync(ct);
        var qualified = await db.BusinessSubServiceAssignments.AsNoTracking().Where(a => services.Contains(a.BusinessSubServiceId) && a.BusinessUser.BusinessId == businessId).Select(a => a.BusinessUserId).Distinct().ToListAsync(ct);
        var result = new List<JobEmployeeOption>();
        foreach (var member in members)
        {
            string? reason = !member.IsActive || member.EndDate != null || member.StartDate > DateTime.UtcNow || member.User.InactiveDate != null ? "Inactive membership" : busy.Contains(member.BusinessUserId) ? "Busy at this time" : services.Count > 0 && !qualified.Contains(member.BusinessUserId) ? "Not qualified for these services" : null;
            for (var day = first; reason == null && day <= last; day = day.AddDays(1))
            {
                var off = timeOff.Where(t => t.IsAllDay == true && t.EffectiveStartDate <= day && t.EffectiveEndDate >= day).Select(t => t.BusinessUserId).ToHashSet();
                if (off.Contains(member.BusinessUserId)) { reason = "Day off"; break; }
                var frames = calculator.GetLatestTimeFramesForMemberOnDate(member.BusinessUserId, day, hours, timeOff, off);
                var dayStart = DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                var covered = Math.Max(0, (job.StartDateTime - dayStart).TotalMinutes);
                var until = Math.Min(1440, (job.EndDateTime - dayStart).TotalMinutes);
                foreach (var frame in frames.OrderBy(f => f.StartTime))
                    if (frame.StartTime.ToTimeSpan().TotalMinutes <= covered) covered = Math.Max(covered, frame.EndTime.ToTimeSpan().TotalMinutes);
                if (covered < until) reason = "Outside available working hours";
            }
            result.Add(new(member.BusinessUserId, (member.User.FirstName + " " + member.User.LastName).Trim(), member.User.ProfileImageBase64, reason ?? "Available", reason == null, current.Contains(member.BusinessUserId)));
        }
        return result;
    }
    public async Task<string?> Validate(int businessId, Job job, IReadOnlyList<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return null;
        if (ids.Count > 100 || ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count) return "Choose up to 100 distinct active employees.";
        var options = await Options(businessId, job, ct);
        foreach (var id in ids)
        {
            var option = options.FirstOrDefault(o => o.BusinessUserId == id);
            if (option == null) return "A selected employee is not an active member of this business.";
            if (!option.CanAssign) return option.DisplayName + ": " + option.Availability + ".";
        }
        var requirements = await db.JobServiceLines.AsNoTracking().Where(l => l.JobId == job.JobId && l.BusinessSubServiceId != null).Select(l => new { Id = l.BusinessSubServiceId!.Value, l.ServiceName, Count = l.BusinessSubService == null ? 1 : l.BusinessSubService.EmployeeCount }).ToListAsync(ct);
        foreach (var service in requirements)
        {
            var count = await db.BusinessSubServiceAssignments.AsNoTracking().Where(a => a.BusinessSubServiceId == service.Id && ids.Contains(a.BusinessUserId) && a.BusinessUser.BusinessId == businessId).Select(a => a.BusinessUserId).Distinct().CountAsync(ct);
            if (count < service.Count) return service.ServiceName + " requires " + service.Count + " qualified employee(s).";
        }
        return null;
    }
}
