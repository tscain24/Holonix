namespace Holonix.Server.Application.Results;

public sealed record GeocodeSuggestionResult(
    string? MapboxId,
    string? FullAddress,
    string Address1,
    string? City,
    string? State,
    string? ZipCode,
    string? CountryCode,
    decimal Latitude,
    decimal Longitude);

