namespace Holonix.Server.Contracts.Business;

public sealed record BusinessHoursDayRequest(
    int DayOfWeek,
    string? OpenTime,
    string? CloseTime,
    bool IsClosed);

public sealed record BusinessHoursDayResponse(
    int DayOfWeek,
    string? OpenTime,
    string? CloseTime,
    bool IsClosed);

