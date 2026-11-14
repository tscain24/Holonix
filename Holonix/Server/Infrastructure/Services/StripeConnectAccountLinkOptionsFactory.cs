using Stripe.V2.Core;

namespace Holonix.Server.Infrastructure.Services;

public static class StripeConnectAccountLinkOptionsFactory
{
    public static AccountLinkCreateOptions Create(
        string connectedAccountId,
        string returnUrl,
        string refreshUrl,
        bool revisitOnboarding)
    {
        _ = revisitOnboarding;

        return new AccountLinkCreateOptions
        {
            Account = connectedAccountId,
            UseCase = new AccountLinkCreateUseCaseOptions
            {
                Type = "account_onboarding",
                AccountOnboarding = new AccountLinkCreateUseCaseAccountOnboardingOptions
                {
                    Configurations = ["recipient"],
                    ReturnUrl = returnUrl,
                    RefreshUrl = refreshUrl,
                    CollectionOptions = new AccountLinkCreateUseCaseAccountOnboardingCollectionOptionsOptions
                    {
                        Fields = "eventually_due",
                        FutureRequirements = "include",
                    },
                },
            },
        };
    }
}
