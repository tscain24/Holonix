using System.ComponentModel.DataAnnotations;
namespace Holonix.Server.Contracts.Insights;

public class InsightsQuery
{
    public DateOnly Start { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
    public DateOnly End { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string GroupBy { get; set; } = "month";
    public string? Status { get; set; }
    public long? EmployeeId { get; set; }
    [StringLength(100)] public string? Customer { get; set; }
    public bool ExactCustomer { get; set; }
    [StringLength(100)] public string? Service { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    [Range(1, 100000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 25;
}
public record InsightGroup(string Key, int Jobs, decimal JobValue, decimal AverageValue, int CompletedJobs, decimal CompletedValue, int CancelledJobs, long? EmployeeId = null);
public record InsightSummary(int Jobs, decimal BookedValue, decimal CompletedValue, decimal AverageValue, int CompletedJobs, int CancelledJobs, decimal UpcomingValue);
public record InsightJob(long JobId, string Name, string Customer, DateTime Start, DateTime End, DateTime? CompletedAt, string Status, decimal Value, string PaymentStatus, string[] Employees, string[] Services);
public record InsightEmployee(long EmployeeId, string Name, int Jobs, decimal BookedValue, decimal CompletedValue, int CompletedJobs, int CancelledJobs);
public record InsightsResponse(InsightSummary Summary, IReadOnlyList<InsightGroup> Groups, IReadOnlyList<InsightJob> Jobs, int TotalJobs, int Page, int PageSize, IReadOnlyList<InsightEmployee> Employees);
