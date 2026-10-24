namespace Holonix.Server.Contracts.Business;

public sealed record ReassignJobRequest(IReadOnlyList<long> BusinessUserIds);
