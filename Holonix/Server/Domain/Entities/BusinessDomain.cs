namespace Holonix.Server.Domain.Entities;

public class Service
{
    public int ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? InactiveDate { get; set; }
}

public class Business
{
    public int BusinessId { get; set; }
    public string BusinessCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateTime? InactiveDate { get; set; }
    public bool IsProductBased { get; set; }
    public bool IsRecurring { get; set; }

    public BusinessDetails? Details { get; set; }
}

public class BusinessDetails
{
    public int BusinessId { get; set; }
    public string? Description { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public int? CountryId { get; set; }
    public string? ZipCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? BusinessIconBase64 { get; set; }
    public decimal BusinessJobPercentage { get; set; }

    public Business Business { get; set; } = null!;
    public Country? Country { get; set; }
}

public class Country
{
    public int CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TwoLetterIsoCode { get; set; } = string.Empty;
    public string? ThreeLetterIsoCode { get; set; }
}

public class BusinessService
{
    public long BusinessServiceId { get; set; }
    public int BusinessId { get; set; }
    public int ServiceId { get; set; }

    public Business Business { get; set; } = null!;
    public Service Service { get; set; } = null!;
}

public class BusinessRole
{
    public long BusinessRoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long HierarchyNumber { get; set; }
}

public static class BusinessRoleNames
{
    public const string Owner = "Owner";
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    public static bool RequiresReportsToUserId(string? roleName)
    {
        return !string.Equals(roleName, Owner, StringComparison.OrdinalIgnoreCase);
    }
}

public class BusinessUser
{
    public long BusinessUserId { get; set; }
    public int BusinessId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? ReportsToUserId { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Business Business { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ApplicationUser? ReportsToUser { get; set; }
}

public class BusinessUserRole
{
    public long BusinessUserRoleId { get; set; }
    public long BusinessUserId { get; set; }
    public long BusinessRoleId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public BusinessUser BusinessUser { get; set; } = null!;
    public BusinessRole BusinessRole { get; set; } = null!;
}

public class BusinessUserAvailability
{
    public long BusinessUserAvailabilityId { get; set; }
    public long BusinessUserId { get; set; }
    public byte DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateOnly EffectiveStartDate { get; set; }
    public DateOnly? EffectiveEndDate { get; set; }

    public BusinessUser BusinessUser { get; set; } = null!;
}

public class BusinessUserTimeOff
{
    public long BusinessUserTimeOffId { get; set; }
    public long BusinessUserId { get; set; }
    public DateOnly? EffectiveStartDate { get; set; }
    public DateOnly? EffectiveEndDate { get; set; }
    public TimeOnly? EffectiveStartTime { get; set; }
    public TimeOnly? EffectiveEndTime { get; set; }
    public bool? IsAllDay { get; set; }

    public BusinessUser BusinessUser { get; set; } = null!;
}

public class Job
{
    public long JobId { get; set; }
    public string? Name { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public decimal Cost { get; set; }
    public decimal NetCost { get; set; }
    public string? UserId { get; set; }
    public int BusinessId { get; set; }
    public long? BusinessUserId { get; set; }
    public bool IsContract { get; set; }
    public long? JobRecurrenceId { get; set; }

    public ApplicationUser? User { get; set; }
    public Business Business { get; set; } = null!;
    public BusinessUser? BusinessUser { get; set; }
    public JobRecurrence? JobRecurrence { get; set; }
}

public class JobWorkload
{
    public long JobWorkloadId { get; set; }
    public long JobId { get; set; }
    public long WorkloadId { get; set; }

    public Job Job { get; set; } = null!;
    public Workload Workload { get; set; } = null!;
}

public class JobRecurrence
{
    public long JobRecurrenceId { get; set; }
    public DateOnly StartEffectiveDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Interval { get; set; }
    public byte FrequencyId { get; set; }
    public DateOnly? EndEffectiveDate { get; set; }
    public bool IsActive { get; set; }
}

public class JobRecurrenceException
{
    public long JobRecurrenceExceptionId { get; set; }
    public DateOnly ExceptionDate { get; set; }
    public DateTime? OverrideStartDateTime { get; set; }
    public DateTime? OverrideEndDateTime { get; set; }
    public long JobRecurrenceId { get; set; }

    public JobRecurrence JobRecurrence { get; set; } = null!;
}

public class EmployeeInvite
{
    public long EmployeeInviteId { get; set; }
    public int BusinessId { get; set; }
    public string TargetEmail { get; set; } = string.Empty;
    public string NormalizedTargetEmail { get; set; } = string.Empty;
    public string? TargetUserId { get; set; }
    public long? TargetBusinessRoleId { get; set; }

    public Business Business { get; set; } = null!;
    public ApplicationUser? TargetUser { get; set; }
    public BusinessRole? TargetBusinessRole { get; set; }
}

public class EmployeeInviteWorkload
{
    public long EmployeeInviteWorkloadId { get; set; }
    public long WorkloadId { get; set; }
    public long EmployeeInviteId { get; set; }

    public Workload Workload { get; set; } = null!;
    public EmployeeInvite EmployeeInvite { get; set; } = null!;
}

public class Workload
{
    public long WorkloadId { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public long WorkloadTypeId { get; set; }
    public long WorkloadStatusId { get; set; }

    public ApplicationUser CreatedByUser { get; set; } = null!;
    public WorkloadType WorkloadType { get; set; } = null!;
    public WorkloadStatus WorkloadStatus { get; set; } = null!;
}

public class WorkloadType
{
    public long WorkloadTypeId { get; set; }
    public string Type { get; set; } = string.Empty;
}

public static class WorkloadTypeNames
{
    public const string EmployeeInvite = "Employee Invite";
}

public class WorkloadStatus
{
    public long WorkloadStatusId { get; set; }
    public long WorkloadTypeId { get; set; }
    public string Status { get; set; } = string.Empty;

    public WorkloadType WorkloadType { get; set; } = null!;
}

public static class WorkloadStatusNames
{
    public const string PendingInvite = "Pending Invite";
    public const string Active = "Active";
    public const string Denied = "Denied";
    public const string Closed = "Closed";
}
