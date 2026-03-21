namespace Holonix.Server.Contracts.Business;

public sealed record CreateBusinessRequest(
    string Name,
    string? Description,
    string Address1,
    string? Address2,
    string City,
    string State,
    string ZipCode,
    int CountryId,
    string? BusinessIconBase64,
    decimal BusinessJobPercentage,
    bool IsProductBased,
    IReadOnlyCollection<int> ServiceIds);
