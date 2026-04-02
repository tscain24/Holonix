namespace Holonix.Server.Contracts.Business;

public sealed record BusinessWorkspaceEmployeePageResponse(
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyList<BusinessWorkspaceEmployeeResponse> Employees);
