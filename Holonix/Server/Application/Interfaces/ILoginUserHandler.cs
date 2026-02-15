using Holonix.Server.Application.Results;
using Holonix.Server.Contracts.Auth;

namespace Holonix.Server.Application.Interfaces;

public interface ILoginUserHandler
{
    Task<HandlerResult<AuthResponse>> HandleAsync(LoginRequest request, CancellationToken cancellationToken);
}

