namespace Holonix.Server.Contracts.Business;

public sealed record MyBusinessAvailabilityResponse(
    long BusinessUserId,
    DateOnly EffectiveStartDate,
    IReadOnlyList<MyBusinessAvailabilityDayResponse> Availability,
    IReadOnlyList<MyBusinessTimeOffResponse> DaysOff);

public sealed record MyBusinessAvailabilityDayResponse(
    byte DayOfWeek,
    TimeOnly? StartTime,
    TimeOnly? EndTime);

public sealed record MyBusinessTimeOffResponse(
    long BusinessUserTimeOffId,
    DateOnly EffectiveStartDate,
    DateOnly EffectiveEndDate,
    TimeOnly StartTime,
    TimeOnly EndTime);
