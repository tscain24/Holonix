using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holonix.Server.Application.Interfaces;
using Holonix.Server.Application.Results;
using Holonix.Server.Domain.Entities;
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

    public async Task<GeocodeAddressResult?> GeocodeBusinessDetailsAsync(BusinessDetails businessDetails, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new InvalidOperationException("Mapbox access token is missing.");
        }

        var query = BuildQuery(businessDetails);
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

    private Dictionary<string, string?> BuildQuery(BusinessDetails businessDetails)
    {
        var query = new Dictionary<string, string?>
        {
            ["address_line1"] = BuildAddressLine1(businessDetails),
            ["place"] = businessDetails.City,
            ["region"] = businessDetails.State,
            ["postcode"] = businessDetails.ZipCode,
            ["country"] = string.IsNullOrWhiteSpace(businessDetails.Country)
                ? _options.CountryCode
                : businessDetails.Country,
            ["autocomplete"] = "false",
            ["permanent"] = _options.PermanentGeocoding ? "true" : "false",
            ["access_token"] = _options.AccessToken,
        };

        return query
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    private static string? BuildAddressLine1(BusinessDetails businessDetails)
    {
        if (string.IsNullOrWhiteSpace(businessDetails.Address1))
        {
            return null;
        }

        return businessDetails.Address1.Trim();
    }

    private sealed class MapboxGeocodeResponse
    {
        [JsonPropertyName("features")]
        public List<MapboxFeature>? Features { get; set; }
    }

    private sealed class MapboxFeature
    {
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
        [JsonPropertyName("full_address")]
        public string? FullAddress { get; set; }

        [JsonPropertyName("place_formatted")]
        public string? PlaceFormatted { get; set; }
    }
}
