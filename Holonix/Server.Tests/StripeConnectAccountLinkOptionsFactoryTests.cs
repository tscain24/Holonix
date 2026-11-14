using Holonix.Server.Infrastructure.Services;
using Xunit;

namespace Holonix.Server.Tests;

public class StripeConnectAccountLinkOptionsFactoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_UsesOnboardingForInitialAndRevisitedOnboarding(bool revisitOnboarding)
    {
        var options = StripeConnectAccountLinkOptionsFactory.Create(
            "acct_test",
            "https://localhost:4200/workspace/payments/BUSINESS?stripe=return",
            "https://localhost:4200/workspace/payments/BUSINESS?stripe=refresh",
            revisitOnboarding);

        Assert.Equal("acct_test", options.Account);
        Assert.Equal("account_onboarding", options.UseCase.Type);
        Assert.Null(options.UseCase.AccountUpdate);
        Assert.NotNull(options.UseCase.AccountOnboarding);
        Assert.Equal(["recipient"], options.UseCase.AccountOnboarding.Configurations);
        Assert.Equal("eventually_due", options.UseCase.AccountOnboarding.CollectionOptions.Fields);
        Assert.Equal("include", options.UseCase.AccountOnboarding.CollectionOptions.FutureRequirements);
    }

    [Fact]
    public void Create_PreservesReturnAndRefreshUrls()
    {
        const string returnUrl = "https://localhost:4200/workspace/payments/BUSINESS?stripe=return";
        const string refreshUrl = "https://localhost:4200/workspace/payments/BUSINESS?stripe=refresh";

        var options = StripeConnectAccountLinkOptionsFactory.Create(
            "acct_test",
            returnUrl,
            refreshUrl,
            revisitOnboarding: true);

        Assert.Equal(returnUrl, options.UseCase.AccountOnboarding.ReturnUrl);
        Assert.Equal(refreshUrl, options.UseCase.AccountOnboarding.RefreshUrl);
    }
}
