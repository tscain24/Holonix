using System.Security.Claims;
using Holonix.Server.Application.Interfaces;
using Holonix.Server.Contracts.Business;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Configuration;
using Holonix.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace Holonix.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/business")]
public sealed class BusinessPaymentAccountController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IStripeConnectService _stripeConnectService;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<BusinessPaymentAccountController> _logger;

    public BusinessPaymentAccountController(
        ApplicationDbContext dbContext,
        IStripeConnectService stripeConnectService,
        IOptions<StripeOptions> stripeOptions,
        ILogger<BusinessPaymentAccountController> logger)
    {
        _dbContext = dbContext;
        _stripeConnectService = stripeConnectService;
        _stripeOptions = stripeOptions.Value;
        _logger = logger;
    }

    [HttpGet("{businessCode}/payment-account")]
    public async Task<IActionResult> GetPaymentAccount(string businessCode, CancellationToken cancellationToken)
    {
        var access = await GetAuthorizedBusinessAsync(businessCode, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var business = access.Business!;
        var stored = await _dbContext.BusinessPaymentAccounts
            .SingleOrDefaultAsync(item => item.BusinessId == business.BusinessId, cancellationToken);
        if (stored is null)
        {
            return Ok(ToResponse(null));
        }

        try
        {
            var stripeAccount = await _stripeConnectService.GetAccountAsync(stored.StripeConnectedAccountId, cancellationToken);
            Apply(stored, stripeAccount);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok(ToResponse(stored));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe Connect account lookup failed for business {BusinessCode}.", business.BusinessCode);
            return StatusCode(StatusCodes.Status502BadGateway, new { errors = new[] { "Stripe account status is temporarily unavailable." } });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Stripe Connect is not configured for business {BusinessCode}.", business.BusinessCode);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { errors = new[] { "Stripe Connect is not configured yet." } });
        }
    }

    [HttpPost("{businessCode}/payment-account/link")]
    public async Task<IActionResult> CreatePaymentAccountLink(string businessCode, CancellationToken cancellationToken)
    {
        var access = await GetAuthorizedBusinessAsync(businessCode, cancellationToken);
        if (access.Error is not null)
        {
            return access.Error;
        }

        var business = access.Business!;
        try
        {
            var stored = await _dbContext.BusinessPaymentAccounts
                .SingleOrDefaultAsync(item => item.BusinessId == business.BusinessId, cancellationToken);

            if (stored is null)
            {
                var stripeAccount = await _stripeConnectService.CreateAccountAsync(
                    new StripeConnectAccountRequest(
                        business.BusinessCode,
                        business.Name,
                        business.Details?.BusinessEmail,
                        business.Details?.BusinessPhoneNumber,
                        business.Details?.Country?.TwoLetterIsoCode),
                    $"holonix-connect-business-{business.BusinessId}",
                    cancellationToken);

                stored = new BusinessPaymentAccount
                {
                    BusinessId = business.BusinessId,
                    StripeConnectedAccountId = stripeAccount.AccountId,
                    CreatedDateTime = DateTime.UtcNow,
                };
                Apply(stored, stripeAccount);
                _dbContext.BusinessPaymentAccounts.Add(stored);

                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    _dbContext.Entry(stored).State = EntityState.Detached;
                    stored = await _dbContext.BusinessPaymentAccounts
                        .SingleAsync(item => item.BusinessId == business.BusinessId, cancellationToken);
                }
            }
            else
            {
                var stripeAccount = await _stripeConnectService.GetAccountAsync(stored.StripeConnectedAccountId, cancellationToken);
                Apply(stored, stripeAccount);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var (returnUrl, refreshUrl) = BuildClientUrls(business.BusinessCode);
            var link = await _stripeConnectService.CreateAccountLinkAsync(
                stored.StripeConnectedAccountId,
                returnUrl,
                refreshUrl,
                stored.Status == BusinessPaymentAccountStatuses.Ready,
                cancellationToken);

            return Ok(new BusinessPaymentAccountLinkResponse(link));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe Connect onboarding failed for business {BusinessCode}.", business.BusinessCode);
            return StatusCode(StatusCodes.Status502BadGateway, new { errors = new[] { "Stripe onboarding is temporarily unavailable. Please try again." } });
        }
        catch (StripeConnectValidationException ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Stripe Connect is not configured for business {BusinessCode}.", business.BusinessCode);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { errors = new[] { "Stripe Connect is not configured yet." } });
        }
    }

    [AllowAnonymous]
    [HttpPost("stripe/connect/webhook")]
    public async Task<IActionResult> StripeConnectWebhook(CancellationToken cancellationToken)
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        string? connectedAccountId;
        try
        {
            connectedAccountId = _stripeConnectService.ParseRelatedAccountId(payload, signature);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Rejected Stripe Connect webhook with an invalid signature or payload.");
            return BadRequest();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Stripe Connect webhook is not configured.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (string.IsNullOrWhiteSpace(connectedAccountId))
        {
            return Ok();
        }

        var stored = await _dbContext.BusinessPaymentAccounts
            .SingleOrDefaultAsync(item => item.StripeConnectedAccountId == connectedAccountId, cancellationToken);
        if (stored is null)
        {
            return Ok();
        }

        try
        {
            var stripeAccount = await _stripeConnectService.GetAccountAsync(connectedAccountId, cancellationToken);
            Apply(stored, stripeAccount);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Could not synchronize Stripe Connect account {ConnectedAccountId}.", connectedAccountId);
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    private async Task<(Business? Business, IActionResult? Error)> GetAuthorizedBusinessAsync(
        string businessCode,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (null, Unauthorized(new { errors = new[] { "Invalid user context." } }));
        }

        var normalizedCode = (businessCode ?? string.Empty).Trim().ToUpperInvariant();
        var business = await _dbContext.Businesses
            .Include(item => item.Details)
            .ThenInclude(details => details!.Country)
            .SingleOrDefaultAsync(item => item.BusinessCode == normalizedCode && item.InactiveDate == null, cancellationToken);
        if (business is null)
        {
            return (null, NotFound(new { errors = new[] { "Business not found." } }));
        }

        var roleName = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(item => item.BusinessId == business.BusinessId && item.UserId == userId && item.IsActive && item.EndDate == null)
            .SelectMany(item => _dbContext.BusinessUserRoles
                .Where(role => role.BusinessUserId == item.BusinessUserId && role.EndDate == null)
                .Select(role => role.BusinessRole.Name))
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.Equals(roleName, BusinessRoleNames.Owner, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(roleName, BusinessRoleNames.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            return (null, StatusCode(StatusCodes.Status403Forbidden, new { errors = new[] { "Only business owners and administrators can manage the payment account." } }));
        }

        return (business, null);
    }

    private (string ReturnUrl, string RefreshUrl) BuildClientUrls(string businessCode)
    {
        if (!Uri.TryCreate(_stripeOptions.ClientBaseUrl, UriKind.Absolute, out var clientBaseUri) ||
            (clientBaseUri.Scheme != Uri.UriSchemeHttps && !clientBaseUri.IsLoopback))
        {
            throw new InvalidOperationException("Stripe ClientBaseUrl must be an absolute HTTPS URL.");
        }

        var path = $"workspace/payments/{Uri.EscapeDataString(businessCode)}";
        return (
            new Uri(clientBaseUri, $"{path}?stripe=return").ToString(),
            new Uri(clientBaseUri, $"{path}?stripe=refresh").ToString());
    }

    private static void Apply(BusinessPaymentAccount stored, StripeConnectAccountResult stripeAccount)
    {
        stored.Status = stripeAccount.Status;
        stored.TransfersCapabilityStatus = stripeAccount.TransfersCapabilityStatus;
        stored.PayoutsCapabilityStatus = stripeAccount.PayoutsCapabilityStatus;
        stored.UpdatedDateTime = DateTime.UtcNow;
    }

    private static BusinessPaymentAccountResponse ToResponse(BusinessPaymentAccount? account)
    {
        var status = account?.Status ?? BusinessPaymentAccountStatuses.NotConnected;
        return new BusinessPaymentAccountResponse(
            status,
            account?.TransfersCapabilityStatus,
            account?.PayoutsCapabilityStatus,
            status == BusinessPaymentAccountStatuses.Ready,
            status switch
            {
                BusinessPaymentAccountStatuses.NotConnected => "Connect with Stripe",
                BusinessPaymentAccountStatuses.SetupIncomplete => "Continue setup",
                BusinessPaymentAccountStatuses.VerificationPending => "Review with Stripe",
                BusinessPaymentAccountStatuses.Ready => "Update Stripe details",
                BusinessPaymentAccountStatuses.ActionRequired => "Resolve with Stripe",
                _ => "Continue with Stripe",
            });
    }
}
