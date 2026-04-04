namespace Holonix.Server.Contracts.Auth;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string DateOfBirth,
    string Email,
    string Password,
    string? ProfileImageBase64);
