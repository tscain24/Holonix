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
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<GeocodingController> _logger;

    public GeocodingController(IGeocodingService geocodingService, ILogger<GeocodingController> logger)
    {
        _geocodingService = geocodingService;
        _logger = logger;
    }

    [Authorize]
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
        var suggestions = await _geocodingService.AutocompleteAsync(trimmed, countryCode, limit, cancellationToken);
        return Ok(suggestions);
    }

    [Authorize]
    [HttpPost("resolve-address")]
    public async Task<ActionResult<GeocodeSuggestionResult?>> ResolveAddress(
        [FromBody] ResolveAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Address1)
            || string.IsNullOrWhiteSpace(request.City)
            || string.IsNullOrWhiteSpace(request.State)
            || string.IsNullOrWhiteSpace(request.ZipCode))
        {
            return BadRequest(new { errors = new[] { "Address is incomplete." } });
        }

        try
        {
            var query = $"{request.Address1.Trim()}, {request.City.Trim()}, {request.State.Trim()} {request.ZipCode.Trim()}";
            var suggestions = await _geocodingService.AutocompleteAsync(
                query,
                string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim(),
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
