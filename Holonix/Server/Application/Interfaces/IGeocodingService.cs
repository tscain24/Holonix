using Holonix.Server.Application.Results;
using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Application.Interfaces;

public interface IGeocodingService
{
    Task<GeocodeAddressResult?> GeocodeBusinessDetailsAsync(BusinessDetails businessDetails, CancellationToken cancellationToken = default);
}
