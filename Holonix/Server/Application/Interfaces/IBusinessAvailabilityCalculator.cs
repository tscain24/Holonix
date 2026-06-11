using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Application.Interfaces;

public interface IBusinessAvailabilityCalculator
{
    IReadOnlyList<AvailabilityTimeFrameResult> GetLatestTimeFramesForMemberOnDate(
        long businessUserId,
        DateOnly date,
        IReadOnlyList<BusinessUserAvailability> availabilityRows,
        IReadOnlyList<BusinessUserTimeOff> timeOffRows,
        ISet<long> allDayOffMemberIds);

    IReadOnlyList<AvailabilityTimeFrameResult> GetMergedTimeFramesForMembersOnDate(
        IReadOnlyList<long> businessUserIds,
        DateOnly date,
        IReadOnlyList<BusinessUserAvailability> availabilityRows,
        IReadOnlyList<BusinessUserTimeOff> timeOffRows,
        ISet<long> allDayOffMemberIds);

    IReadOnlyList<AvailabilityTimeFrameResult> GetQualifiedTimeFramesForMembersOnDate(
        IReadOnlyList<long> businessUserIds,
        DateOnly date,
        IReadOnlyList<BusinessUserAvailability> availabilityRows,
        IReadOnlyList<BusinessUserTimeOff> timeOffRows,
        ISet<long> allDayOffMemberIds,
        int requiredMemberCount);

    IReadOnlyList<AvailabilityTimeFrameResult> GetMergedWeeklyTimeFramesForDay(
        IReadOnlyList<long> businessUserIds,
        int dayOfWeek,
        DateOnly referenceDate,
        IReadOnlyList<BusinessUserAvailability> availabilityRows);
}

public sealed record AvailabilityTimeFrameResult(
    TimeOnly StartTime,
    TimeOnly EndTime);
