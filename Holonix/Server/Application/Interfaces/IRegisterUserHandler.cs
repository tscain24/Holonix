using Holonix.Server.Application.Results;
using Holonix.Server.Contracts.Auth;

namespace Holonix.Server.Application.Interfaces;

public interface IRegisterUserHandler
{
    Task<HandlerResult<RegisterResponse>> HandleAsync(RegisterRequest request, CancellationToken cancellationToken);
}

