namespace Holonix.Server.Contracts.Auth;

public record UpdateUserProfileRequest(string FirstName, string LastName, string? DateOfBirth, string? PhoneNumber);
