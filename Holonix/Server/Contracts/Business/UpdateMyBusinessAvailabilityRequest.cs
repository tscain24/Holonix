namespace Holonix.Server.Contracts.Business;

public sealed record UpdateMyBusinessAvailabilityRequest(
    IReadOnlyList<UpdateMyBusinessAvailabilityDayRequest>? Availability,
    IReadOnlyList<UpdateMyBusinessTimeOffRequest>? DaysOff);

public sealed record UpdateMyBusinessAvailabilityDayRequest(
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record UpdateMyBusinessTimeOffRequest(
    DateOnly EffectiveStartDate,
    DateOnly EffectiveEndDate,
    TimeOnly StartTime,
    TimeOnly EndTime);
