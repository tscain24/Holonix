namespace Holonix.Server.Contracts.Business;

public sealed record UserBusinessSummaryResponse(
    int BusinessId,
    string Name,
    string? Description,
    string? City,
    string? State,
    string? CountryName,
    string? BusinessIconBase64,
    decimal BusinessJobPercentage,
    bool IsProductBased,
    int ServiceCount,
    string? RoleName,
    int TotalJobs);
