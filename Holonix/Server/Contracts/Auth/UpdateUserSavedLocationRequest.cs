namespace Holonix.Server.Contracts.Auth;

public sealed record UpdateUserSavedLocationRequest(
    string Label,
    string Address1,
    string? Address2,
    string City,
    string State,
    string ZipCode,
    decimal? Latitude,
    decimal? Longitude,
    bool IsPrimary);
