namespace Holonix.Server.Contracts.Auth;

public sealed record UserSavedLocationResponse(
    long UserSavedLocationId,
    string Label,
    string Address1,
    string? Address2,
    string City,
    string State,
    string ZipCode,
    decimal? Latitude,
    decimal? Longitude,
    bool IsPrimary);
