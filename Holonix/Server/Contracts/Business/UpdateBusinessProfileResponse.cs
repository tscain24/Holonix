namespace Holonix.Server.Contracts.Business;

public sealed record UpdateBusinessProfileResponse(
    int BusinessId,
    string Name,
    string? Description,
    string? BusinessIconBase64,
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? ZipCode,
    string? CountryName,
    decimal BusinessJobPercentage);
