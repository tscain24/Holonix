namespace Holonix.Server.Application.Results;

public sealed record GeocodeAddressResult(
    decimal Latitude,
    decimal Longitude,
    string? FormattedAddress);
