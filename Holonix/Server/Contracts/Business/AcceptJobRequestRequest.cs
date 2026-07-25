namespace Holonix.Server.Contracts.Business;

public sealed record AcceptJobRequestRequest(IReadOnlyList<long> BusinessUserIds);
