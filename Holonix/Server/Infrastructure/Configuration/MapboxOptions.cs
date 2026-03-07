namespace Holonix.Server.Infrastructure.Configuration;

public sealed class MapboxOptions
{
    public const string SectionName = "Mapbox";

    public string AccessToken { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.mapbox.com";
    public bool PermanentGeocoding { get; set; } = true;
    public string CountryCode { get; set; } = "us";
}
