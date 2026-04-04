namespace Holonix.Server.Contracts.Business;

public sealed record CreateBusinessSubServiceRequest(
    string Name,
    DateOnly EffectiveDate);
