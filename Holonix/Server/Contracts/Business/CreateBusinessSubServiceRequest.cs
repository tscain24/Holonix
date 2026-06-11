namespace Holonix.Server.Contracts.Business;

public sealed record CreateBusinessSubServiceRequest(
    string Name,
    string? Description,
    bool RequiresServiceLocation,
    bool ConsultationNeeded,
    int DurationMinutes,
    decimal Price,
    int EmployeeCount,
    IReadOnlyList<long>? AssignedBusinessUserIds,
    DateOnly EffectiveDate);
