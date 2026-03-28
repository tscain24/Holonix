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
    private readonly IQueryableUserStore<ApplicationUser> _userStore;
    private readonly ITokenService _tokenService;

    public LoginUserHandler(
        UserManager<ApplicationUser> userManager,
        IQueryableUserStore<ApplicationUser> userStore,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _userStore = userStore;
        _tokenService = tokenService;
    }

    public async Task<HandlerResult<AuthResponse>> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return HandlerResult<AuthResponse>.Fail(StatusCodes.Status400BadRequest, "Email and password are required.");
        }

        var normalizedEmail = _userManager.NormalizeEmail(request.Email.Trim());
        var user = await _userStore.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail && candidate.InactiveDate == null,
                cancellationToken);

        if (user is null)
        {
            return HandlerResult<AuthResponse>.Fail(StatusCodes.Status401Unauthorized, "Invalid credentials.");
        }

        var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValid)
        {
            return HandlerResult<AuthResponse>.Fail(StatusCodes.Status401Unauthorized, "Invalid credentials.");
        }

        var token = await _tokenService.CreateTokenAsync(user, cancellationToken);
        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        return HandlerResult<AuthResponse>.Ok(
            new AuthResponse(user.Id, displayName, token, user.ProfileImageBase64));
    }
}

