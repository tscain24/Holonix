namespace Holonix.Server.Contracts.Business;

public sealed record UpdateBusinessSubServiceRequest(
    string Name,
    int ServiceId,
    string? Description,
    bool ConsultationNeeded,
    int DurationMinutes,
    decimal Price,
    int EmployeeCount,
    IReadOnlyList<long>? AssignedBusinessUserIds,
    DateOnly EffectiveDate);
