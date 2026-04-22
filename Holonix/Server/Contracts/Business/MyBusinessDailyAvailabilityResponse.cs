namespace Holonix.Server.Contracts.Business;

public sealed record MyBusinessDailyAvailabilityResponse(
    DateOnly Date,
    bool IsAvailable,
    IReadOnlyList<MyBusinessDailyAvailabilityTimeFrameResponse> TimeFrames,
    bool IsDayOff);

public sealed record MyBusinessDailyAvailabilityTimeFrameResponse(
    TimeOnly StartTime,
    TimeOnly EndTime);
