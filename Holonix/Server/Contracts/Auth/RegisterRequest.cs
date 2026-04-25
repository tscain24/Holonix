namespace Holonix.Server.Contracts.Auth;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string DateOfBirth,
    string Email,
    string Password,
    string? ProfileImageBase64,
    string Address1,
    string? Address2,
    string City,
    string State,
    string ZipCode,
    decimal? Latitude,
    decimal? Longitude);
