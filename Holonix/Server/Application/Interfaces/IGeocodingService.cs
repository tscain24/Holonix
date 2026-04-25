using Holonix.Server.Application.Results;

namespace Holonix.Server.Application.Interfaces;

public interface IGeocodingService
{
    Task<GeocodeAddressResult?> GeocodeAsync(GeocodeAddressRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeocodeSuggestionResult>> AutocompleteAsync(
        string query,
        string? countryCode = null,
        int limit = 5,
        CancellationToken cancellationToken = default);
}
