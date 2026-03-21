using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Application.Interfaces;

public interface ITokenService
{
    Task<string> CreateTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}

