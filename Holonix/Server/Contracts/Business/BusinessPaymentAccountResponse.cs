namespace Holonix.Server.Contracts.Business;

public sealed record BusinessPaymentAccountResponse(
    string Status,
    string? TransfersCapabilityStatus,
    string? PayoutsCapabilityStatus,
    bool CanManage,
    string ActionLabel);

public sealed record BusinessPaymentAccountLinkResponse(string Url);
