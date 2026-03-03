namespace Holonix.Server.Contracts.Auth;

public record UserProfileResponse(
    string FirstName,
    string LastName,
    string? DateOfBirth,
    string Email,
    string? ProfileImageBase64);
