namespace Holonix.Server.Contracts.Geocode;

public sealed record ResolveAddressRequest(
    string Address1,
    string? Address2,
    string City,
    string State,
    string ZipCode,
    string? CountryCode);

