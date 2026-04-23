namespace Holonix.Server.Contracts.Business;

public sealed record BusinessDailyTeamAvailabilityResponse(
    DateOnly Date,
    IReadOnlyList<BusinessDailyTeamAvailabilityMemberResponse> Members);

public sealed record BusinessDailyTeamAvailabilityMemberResponse(
    long BusinessUserId,
    string FirstName,
    string LastName,
    bool IsAvailable,
    IReadOnlyList<BusinessDailyTeamAvailabilityTimeFrameResponse> TimeFrames,
    bool IsDayOff);

public sealed record BusinessDailyTeamAvailabilityTimeFrameResponse(
    TimeOnly StartTime,
    TimeOnly EndTime);

