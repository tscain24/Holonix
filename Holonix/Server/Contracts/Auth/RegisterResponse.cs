namespace Holonix.Server.Contracts.Auth;

public record RegisterResponse(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? ProfileImageBase64);
