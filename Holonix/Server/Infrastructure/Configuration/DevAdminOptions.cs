namespace Holonix.Server.Infrastructure.Configuration;

public sealed class DevAdminOptions
{
    public const string SectionName = "DevAdmin";

    public string ApiKey { get; init; } = string.Empty;
}

