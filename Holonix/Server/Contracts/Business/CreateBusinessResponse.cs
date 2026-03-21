namespace Holonix.Server.Contracts.Business;

public sealed record CreateBusinessResponse(
    int BusinessId,
    string Name,
    string UserId,
    string DisplayName,
    string Token,
    string? ProfileImageBase64);
