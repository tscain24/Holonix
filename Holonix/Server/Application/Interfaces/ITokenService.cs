using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Application.Interfaces;

public interface ITokenService
{
    string CreateToken(ApplicationUser user);
}

