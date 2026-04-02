namespace Holonix.Server.Contracts.Business;

public sealed record BusinessWorkspaceResponse(
    int BusinessId,
    string Name,
    string? Description,
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? ZipCode,
    string? CountryName,
    string? BusinessIconBase64,
    decimal BusinessJobPercentage,
    bool IsProductBased,
    string? CurrentUserRoleName,
    IReadOnlyList<string> AvailableEmployeeRoles,
    IReadOnlyList<string> FilterEmployeeRoles,
    int ServiceCount,
    IReadOnlyList<BusinessWorkspaceServiceResponse> Services,
    int ActiveEmployeeCount,
    int TotalJobs,
    decimal TotalRevenue,
    IReadOnlyList<BusinessWorkspaceEmployeeResponse> Employees,
    IReadOnlyList<BusinessWorkspaceJobResponse> Jobs);

public sealed record BusinessWorkspaceServiceResponse(
    int ServiceId,
    string Name);

public sealed record BusinessWorkspaceEmployeeResponse(
    long BusinessUserId,
    string DisplayName,
    string? RoleName,
    DateTime HiredDate,
    bool IsActive,
    int AssignedJobCount,
    bool CanDeactivate,
    bool CanUpdateRole);

public sealed record BusinessWorkspaceJobResponse(
    long JobId,
    string Name,
    DateTime StartDateTime,
    DateTime EndDateTime,
    decimal Cost,
    decimal NetCost,
    string? AssignedEmployeeName);
