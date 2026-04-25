namespace Holonix.Server.Application.Results;

public sealed record GeocodeAddressRequest(
    string Address1,
    string? Address2,
    string City,
    string State,
    string ZipCode,
    string? CountryCode = null);

