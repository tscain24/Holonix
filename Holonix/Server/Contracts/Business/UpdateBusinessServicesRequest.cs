namespace Holonix.Server.Contracts.Business;

public sealed record UpdateBusinessServicesRequest(
    IReadOnlyCollection<int> ServiceIds);
