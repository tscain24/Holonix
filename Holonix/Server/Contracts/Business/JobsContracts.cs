using System.ComponentModel.DataAnnotations;
namespace Holonix.Server.Contracts.Business;

public sealed class JobsQuery
{
    [Range(1, 100000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 25;
    [StringLength(100)] public string? Search { get; set; }
    public string Filter { get; set; } = "all";
    public DateOnly? Start { get; set; }
    public DateOnly? End { get; set; }
    public long? EmployeeId { get; set; }
}
public sealed record JobCommand(string? Revision = null);
public sealed record JobTeamCommand(IReadOnlyList<long> BusinessUserIds, string? Revision = null);
public sealed record JobScheduleCommand(DateTime StartDateTime, DateTime EndDateTime, string? Revision = null);
public sealed class CreateManualJobRequest
{
    [Required, StringLength(200)] public string Name { get; set; } = "";
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    [StringLength(450)] public string? CustomerUserId { get; set; }
    public ManualJobLocation? ServiceLocation { get; set; }
    public IReadOnlyList<long> BusinessSubServiceIds { get; set; } = [];
    public IReadOnlyList<long> BusinessUserIds { get; set; } = [];
}
public sealed class ManualJobLocation
{
    [Required, StringLength(200)] public string Address1 { get; set; } = "";
    [StringLength(200)] public string? Address2 { get; set; }
    [Required, StringLength(100)] public string City { get; set; } = "";
    [Required, StringLength(100)] public string State { get; set; } = "";
    [Required, StringLength(20)] public string ZipCode { get; set; } = "";
}
public sealed record JobsSummary(int Total, int Upcoming, int InProgress, int Unassigned, int Completed, int Cancelled, int Overdue, decimal? TotalValue);
public sealed record OperationalJob(long JobId, string Name, string CustomerName, string? CustomerEmail, string? CustomerPhone,
    DateTime StartDateTime, DateTime EndDateTime, string Status, decimal? Cost, string? PaymentStatus,
    IReadOnlyList<BusinessWorkspaceJobAssignmentResponse> AssignedEmployees, BusinessWorkspaceJobLocationResponse? ServiceLocation,
    IReadOnlyList<string> Services, string? CompletedByName, DateTime? CompletedDateTime, string Revision,
    bool CanReady, bool CanStart, bool CanComplete, bool CanReassign, bool CanReschedule, bool CanCancel, string? CancellationRestriction);
public sealed record JobsPage(JobsSummary Summary, IReadOnlyList<OperationalJob> Jobs, int Total, int Page, int PageSize,
    IReadOnlyList<OperationalJob> Today, int TodayTotal, bool CanManage, bool CanViewFinancials, bool CanCreate, string BusinessCode);
public sealed record JobEmployeeOption(long BusinessUserId, string DisplayName, string? ProfileImageBase64, string Availability, bool CanAssign, bool IsCurrent);
public sealed record JobServiceOption(long BusinessSubServiceId, string Name, decimal? Price, int DurationMinutes, bool RequiresServiceLocation);
public sealed record JobCustomerOption(string UserId, string Name);
public sealed record JobOptions(IReadOnlyList<JobEmployeeOption> Employees, IReadOnlyList<JobServiceOption> Services, IReadOnlyList<JobCustomerOption> Customers);
