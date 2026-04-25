using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holonix.Server.Application.Interfaces;
using Holonix.Server.Application.Results;
using Holonix.Server.Infrastructure.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Holonix.Server.Infrastructure.Services;

public sealed class MapboxGeocodingService : IGeocodingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly MapboxOptions _options;

    public MapboxGeocodingService(HttpClient httpClient, IOptions<MapboxOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<GeocodeAddressResult?> GeocodeAsync(GeocodeAddressRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new InvalidOperationException("Mapbox access token is missing.");
        }

        var query = BuildQuery(request);
        var requestUri = QueryHelpers.AddQueryString("/search/geocode/v6/forward", query);

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var geocodeResponse = await JsonSerializer.DeserializeAsync<MapboxGeocodeResponse>(responseStream, JsonOptions, cancellationToken);
        var feature = geocodeResponse?.Features?.FirstOrDefault();
        var coordinates = feature?.Geometry?.Coordinates;

        if (coordinates is null || coordinates.Count < 2)
        {
            return null;
        }

        var longitude = Convert.ToDecimal(coordinates[0], CultureInfo.InvariantCulture);
        var latitude = Convert.ToDecimal(coordinates[1], CultureInfo.InvariantCulture);

        return new GeocodeAddressResult(
            Latitude: latitude,
            Longitude: longitude,
            FormattedAddress: feature?.Properties?.FullAddress ?? feature?.Properties?.PlaceFormatted);
    }

    public async Task<IReadOnlyList<GeocodeSuggestionResult>> AutocompleteAsync(
        string query,
        string? countryCode = null,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new InvalidOperationException("Mapbox access token is missing.");
        }

        limit = Math.Clamp(limit, 1, 10);

        var requestUri = QueryHelpers.AddQueryString("/search/geocode/v6/forward", new Dictionary<string, string?>
        {
            ["q"] = query.Trim(),
            ["autocomplete"] = "true",
            ["types"] = "address",
            ["limit"] = limit.ToString(CultureInfo.InvariantCulture),
            ["country"] = NormalizeOptional(countryCode) ?? _options.CountryCode,
            ["permanent"] = _options.PermanentGeocoding ? "true" : "false",
            ["access_token"] = _options.AccessToken,
        }.Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value));

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var geocodeResponse = await JsonSerializer.DeserializeAsync<MapboxGeocodeResponse>(responseStream, JsonOptions, cancellationToken);
        var features = geocodeResponse?.Features ?? new List<MapboxFeature>();

        return features
            .Select(feature =>
            {
                var coordinates = feature.Geometry?.Coordinates;
                if (coordinates is null || coordinates.Count < 2)
                {
                    return null;
                }

                var longitude = Convert.ToDecimal(coordinates[0], CultureInfo.InvariantCulture);
                var latitude = Convert.ToDecimal(coordinates[1], CultureInfo.InvariantCulture);
                var properties = feature.Properties;
                var context = properties?.Context;

                var address1 = (properties?.NamePreferred ?? properties?.Name ?? properties?.FullAddress ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(address1))
                {
                    return null;
                }

                var state = NormalizeOptional(context?.Region?.RegionCode) ?? NormalizeOptional(context?.Region?.Name);

                return new GeocodeSuggestionResult(
                    MapboxId: properties?.MapboxId ?? feature.Id,
                    FullAddress: NormalizeOptional(properties?.FullAddress) ?? NormalizeOptional(properties?.PlaceFormatted),
                    Address1: address1,
                    City: NormalizeOptional(context?.Place?.Name),
                    State: state,
                    ZipCode: NormalizeOptional(context?.Postcode?.Name),
                    CountryCode: NormalizeOptional(context?.Country?.CountryCode),
                    Latitude: latitude,
                    Longitude: longitude);
            })
            .Where(item => item is not null)
            .Cast<GeocodeSuggestionResult>()
            .ToList();
    }

    private Dictionary<string, string?> BuildQuery(GeocodeAddressRequest request)
    {
        var query = new Dictionary<string, string?>
        {
            ["address_line1"] = NormalizeRequired(request.Address1),
            ["address_line2"] = NormalizeOptional(request.Address2),
            ["place"] = NormalizeRequired(request.City),
            ["region"] = NormalizeRequired(request.State),
            ["postcode"] = NormalizeRequired(request.ZipCode),
            ["country"] = NormalizeOptional(request.CountryCode) ?? _options.CountryCode,
            ["autocomplete"] = "false",
            ["permanent"] = _options.PermanentGeocoding ? "true" : "false",
            ["access_token"] = _options.AccessToken,
        };

        return query
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private sealed class MapboxGeocodeResponse
    {
        [JsonPropertyName("features")]
        public List<MapboxFeature>? Features { get; set; }
    }

    private sealed class MapboxFeature
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("geometry")]
        public MapboxGeometry? Geometry { get; set; }

        [JsonPropertyName("properties")]
        public MapboxProperties? Properties { get; set; }
    }

    private sealed class MapboxGeometry
    {
        [JsonPropertyName("coordinates")]
        public List<double>? Coordinates { get; set; }
    }

    private sealed class MapboxProperties
    {
        [JsonPropertyName("mapbox_id")]
        public string? MapboxId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("name_preferred")]
        public string? NamePreferred { get; set; }

        [JsonPropertyName("full_address")]
        public string? FullAddress { get; set; }

        [JsonPropertyName("place_formatted")]
        public string? PlaceFormatted { get; set; }

        [JsonPropertyName("context")]
        public MapboxContext? Context { get; set; }
    }

    private sealed class MapboxContext
    {
        [JsonPropertyName("country")]
        public MapboxContextItem? Country { get; set; }

        [JsonPropertyName("region")]
        public MapboxContextRegionItem? Region { get; set; }

        [JsonPropertyName("postcode")]
        public MapboxContextItem? Postcode { get; set; }

        [JsonPropertyName("place")]
        public MapboxContextItem? Place { get; set; }
    }

    private sealed class MapboxContextItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }
    }

    private sealed class MapboxContextRegionItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("region_code")]
        public string? RegionCode { get; set; }
    }
}
