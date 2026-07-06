using Holonix.Server.Application.Interfaces;
using Holonix.Server.Application.Results;
using Holonix.Server.Contracts.Geocode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Holonix.Server.Controllers;

[ApiController]
[Route("api/geocode")]
public sealed class GeocodingController : ControllerBase
{
    private const string AddressTypes = "address,street";
    private const string LocationTypes = "address,street,place,postcode,locality,neighborhood,region";

    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<GeocodingController> _logger;

    public GeocodingController(IGeocodingService geocodingService, ILogger<GeocodingController> logger)
    {
        _geocodingService = geocodingService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("address-suggestions")]
    public async Task<IActionResult> GetAddressSuggestions(
        [FromQuery] string q,
        [FromQuery] string? countryCode,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var trimmed = q?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Ok(Array.Empty<object>());
        }

        var startsWithDigit = trimmed.Length > 0 && char.IsDigit(trimmed[0]);
        if (trimmed.Length < (startsWithDigit ? 2 : 3))
        {
            return Ok(Array.Empty<object>());
        }

        limit = Math.Clamp(limit, 1, 10);
        try
        {
            var suggestions = await _geocodingService.AutocompleteAsync(trimmed, countryCode, AddressTypes, limit, cancellationToken);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load address suggestions for {Query}.", trimmed);
            return Ok(Array.Empty<object>());
        }
    }

    [AllowAnonymous]
    [HttpGet("location-suggestions")]
    public async Task<IActionResult> GetLocationSuggestions(
        [FromQuery] string q,
        [FromQuery] string? countryCode,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var trimmed = q?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Ok(Array.Empty<object>());
        }

        var startsWithDigit = trimmed.Length > 0 && char.IsDigit(trimmed[0]);
        if (trimmed.Length < (startsWithDigit ? 2 : 3))
        {
            return Ok(Array.Empty<object>());
        }

        limit = Math.Clamp(limit, 1, 10);
        try
        {
            var suggestions = await _geocodingService.AutocompleteAsync(trimmed, countryCode, LocationTypes, limit, cancellationToken);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load location suggestions for {Query}.", trimmed);
            return Ok(Array.Empty<object>());
        }
    }

    [Authorize]
    [HttpPost("resolve-address")]
    public async Task<ActionResult<GeocodeSuggestionResult?>> ResolveAddress(
        [FromBody] ResolveAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var address1 = request.Address1?.Trim() ?? string.Empty;
        var city = request.City?.Trim() ?? string.Empty;
        var state = request.State?.Trim() ?? string.Empty;
        var zipCode = request.ZipCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
        {
            return BadRequest(new { errors = new[] { "Location is incomplete." } });
        }

        try
        {
            var queryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(address1))
            {
                queryParts.Add(address1);
            }

            queryParts.Add($"{city}, {state}");

            if (!string.IsNullOrWhiteSpace(zipCode))
            {
                queryParts.Add(zipCode);
            }

            var query = string.Join(", ", queryParts);
            var types = string.IsNullOrWhiteSpace(address1) ? LocationTypes : "address";
            var suggestions = await _geocodingService.AutocompleteAsync(
                query,
                string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim(),
                types,
                limit: 1,
                cancellationToken);

            var bestMatch = suggestions.FirstOrDefault();
            return Ok(bestMatch);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve address geocoding request.");
            return Ok(null);
        }
    }
}
