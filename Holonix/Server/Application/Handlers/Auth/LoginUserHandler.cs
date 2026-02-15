using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Holonix.Server.Application.Interfaces;
using Holonix.Server.Application.Results;
using Holonix.Server.Contracts.Auth;
using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Application.Handlers.Auth;

public sealed class LoginUserHandler : ILoginUserHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public LoginUserHandler(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<HandlerResult<AuthResponse>> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return HandlerResult<AuthResponse>.Fail(StatusCodes.Status400BadRequest, "Email and password are required.");
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return HandlerResult<AuthResponse>.Fail(StatusCodes.Status401Unauthorized, "Invalid credentials.");
        }

        var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValid)
        {
            return HandlerResult<AuthResponse>.Fail(StatusCodes.Status401Unauthorized, "Invalid credentials.");
        }

        var token = _tokenService.CreateToken(user);
        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        return HandlerResult<AuthResponse>.Ok(new AuthResponse(user.Id, displayName, token));
    }
}

