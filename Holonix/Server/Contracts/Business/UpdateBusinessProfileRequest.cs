namespace Holonix.Server.Contracts.Business;

public sealed record UpdateBusinessProfileRequest(
    string Name,
    string? Description,
    string? BusinessIconBase64,
    string? BusinessEmail,
    string? BusinessPhoneNumber,
    string Address1,
    string? Address2,
    string City,
    string State,
    string ZipCode,
    decimal BusinessJobPercentage);
