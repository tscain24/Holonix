namespace Holonix.Server.Contracts.Auth;

public record RegisterRequest(string FirstName, string LastName, string DateOfBirth, string Email, string Password);

