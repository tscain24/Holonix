namespace Holonix.Server.Contracts.Business;

public sealed record PublicBusinessProfileResponse(
    int BusinessId,
    string BusinessCode,
    string Name,
    string? Description,
    string? CategoryName,
    string? City,
    string? State,
    string? CountryName,
    string? BusinessEmail,
    string? BusinessPhoneNumber,
    IReadOnlyList<BusinessHoursDayResponse>? BusinessHours,
    IReadOnlyList<PublicBusinessAvailabilityDayResponse>? Availability,
    string? Address1,
    string? Address2,
    string? ZipCode,
    string? BusinessIconBase64,
    IReadOnlyList<PublicBusinessServiceResponse> Services);

public sealed record PublicBusinessAvailabilityDayResponse(
    int DayOfWeek,
    IReadOnlyList<PublicBusinessAvailabilityTimeFrameResponse> TimeFrames,
    bool IsClosed);

public sealed record PublicBusinessAvailabilityTimeFrameResponse(
    string StartTime,
    string EndTime);

public sealed record PublicBusinessDailyAvailabilityResponse(
    DateOnly Date,
    bool IsAvailable,
    IReadOnlyList<PublicBusinessAvailabilityTimeFrameResponse> TimeFrames);

public sealed record PublicBusinessServiceResponse(
    int ServiceId,
    string Name,
    IReadOnlyList<PublicBusinessSubServiceResponse> SubServices);

public sealed record PublicBusinessSubServiceResponse(
    long BusinessSubServiceId,
    string Name,
    string? Description,
    bool RequiresServiceLocation,
    bool ConsultationNeeded,
    int DurationMinutes,
    decimal Price);
