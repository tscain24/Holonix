using Holonix.Server.Application.Interfaces;
using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Infrastructure.Services;

public sealed class BusinessAvailabilityCalculator : IBusinessAvailabilityCalculator
{
    public IReadOnlyList<AvailabilityTimeFrameResult> GetLatestTimeFramesForMemberOnDate(
        long businessUserId,
        DateOnly date,
        IReadOnlyList<BusinessUserAvailability> availabilityRows,
        IReadOnlyList<BusinessUserTimeOff> timeOffRows,
        ISet<long> allDayOffMemberIds)
    {
        if (allDayOffMemberIds.Contains(businessUserId))
        {
            return Array.Empty<AvailabilityTimeFrameResult>();
        }

        var matches = availabilityRows
            .Where(item =>
                item.BusinessUserId == businessUserId &&
                item.DayOfWeek == (byte)date.DayOfWeek &&
                item.EffectiveStartDate <= date &&
                (!item.EffectiveEndDate.HasValue || item.EffectiveEndDate.Value >= date))
            .ToList();

        if (matches.Count == 0)
        {
            return Array.Empty<AvailabilityTimeFrameResult>();
        }

        var effectiveStartDate = matches.Max(item => item.EffectiveStartDate);
        var availabilityFrames = matches
            .Where(item => item.EffectiveStartDate == effectiveStartDate)
            .OrderBy(item => item.StartTime)
            .ThenBy(item => item.EndTime)
            .Select(item => new AvailabilityTimeFrameResult(item.StartTime, item.EndTime))
            .ToList();

        var timeOffFrames = timeOffRows
            .Where(item =>
                item.BusinessUserId == businessUserId &&
                item.EffectiveStartDate.HasValue &&
                item.EffectiveEndDate.HasValue &&
                item.EffectiveStartDate.Value <= date &&
                item.EffectiveEndDate.Value >= date &&
                item.IsAllDay != true &&
                item.EffectiveStartTime.HasValue &&
                item.EffectiveEndTime.HasValue &&
                item.EffectiveStartTime.Value < item.EffectiveEndTime.Value)
            .Select(item => new AvailabilityTimeFrameResult(
                item.EffectiveStartTime!.Value,
                item.EffectiveEndTime!.Value))
            .ToList();

        return Subtract(availabilityFrames, timeOffFrames);
    }

    public IReadOnlyList<AvailabilityTimeFrameResult> GetMergedTimeFramesForMembersOnDate(
        IReadOnlyList<long> businessUserIds,
        DateOnly date,
        IReadOnlyList<BusinessUserAvailability> availabilityRows,
        IReadOnlyList<BusinessUserTimeOff> timeOffRows,
        ISet<long> allDayOffMemberIds)
    {
        var frames = businessUserIds
            .SelectMany(businessUserId => GetLatestTimeFramesForMemberOnDate(
                businessUserId,
                date,
                availabilityRows,
                timeOffRows,
                allDayOffMemberIds))
            .ToList();

        return Merge(frames);
    }

    public IReadOnlyList<AvailabilityTimeFrameResult> GetQualifiedTimeFramesForMembersOnDate(
        IReadOnlyList<long> businessUserIds,
        DateOnly date,
        IReadOnlyList<BusinessUserAvailability> availabilityRows,
        IReadOnlyList<BusinessUserTimeOff> timeOffRows,
        ISet<long> allDayOffMemberIds,
        int requiredMemberCount)
    {
        if (requiredMemberCount <= 1)
        {
            return GetMergedTimeFramesForMembersOnDate(
                businessUserIds,
                date,
                availabilityRows,
                timeOffRows,
                allDayOffMemberIds);
        }

        if (businessUserIds.Count == 0 || requiredMemberCount > businessUserIds.Count)
        {
            return Array.Empty<AvailabilityTimeFrameResult>();
        }

        var events = new List<(int Time, int Delta)>();
        foreach (var businessUserId in businessUserIds)
        {
            var frames = GetLatestTimeFramesForMemberOnDate(
                businessUserId,
                date,
                availabilityRows,
                timeOffRows,
                allDayOffMemberIds);

            foreach (var frame in frames)
            {
                events.Add((ToMinutes(frame.StartTime), 1));
                events.Add((ToMinutes(frame.EndTime), -1));
            }
        }

        if (events.Count == 0)
        {
            return Array.Empty<AvailabilityTimeFrameResult>();
        }

        var orderedEvents = events
            .OrderBy(item => item.Time)
            .ThenBy(item => item.Delta)
            .ToList();

        var result = new List<AvailabilityTimeFrameResult>();
        var currentCount = 0;
        int? previousTime = null;
        foreach (var group in orderedEvents.GroupBy(item => item.Time))
        {
            var currentTime = group.Key;
            if (previousTime.HasValue && previousTime.Value < currentTime && currentCount >= requiredMemberCount)
            {
                result.Add(new AvailabilityTimeFrameResult(
                    TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(previousTime.Value)),
                    TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(currentTime))));
            }

            currentCount += group.Sum(item => item.Delta);
            previousTime = currentTime;
        }

        return Merge(result);
    }

    public IReadOnlyList<AvailabilityTimeFrameResult> GetMergedWeeklyTimeFramesForDay(
        IReadOnlyList<long> businessUserIds,
        int dayOfWeek,
        DateOnly referenceDate,
        IReadOnlyList<BusinessUserAvailability> availabilityRows)
    {
        var frames = new List<AvailabilityTimeFrameResult>();
        foreach (var businessUserId in businessUserIds)
        {
            var matches = availabilityRows
                .Where(item =>
                    item.BusinessUserId == businessUserId &&
                    item.DayOfWeek == dayOfWeek &&
                    item.EffectiveStartDate <= referenceDate &&
                    (!item.EffectiveEndDate.HasValue || item.EffectiveEndDate.Value >= referenceDate))
                .ToList();

            if (matches.Count == 0)
            {
                continue;
            }

            var effectiveStartDate = matches.Max(item => item.EffectiveStartDate);
            frames.AddRange(matches
                .Where(item => item.EffectiveStartDate == effectiveStartDate)
                .OrderBy(item => item.StartTime)
                .ThenBy(item => item.EndTime)
                .Select(item => new AvailabilityTimeFrameResult(item.StartTime, item.EndTime)));
        }

        return Merge(frames);
    }

    private static IReadOnlyList<AvailabilityTimeFrameResult> Merge(
        IReadOnlyList<AvailabilityTimeFrameResult> frames)
    {
        if (frames.Count == 0)
        {
            return Array.Empty<AvailabilityTimeFrameResult>();
        }

        var ordered = frames
            .Where(frame => frame.StartTime < frame.EndTime)
            .OrderBy(frame => frame.StartTime)
            .ThenBy(frame => frame.EndTime)
            .ToList();

        if (ordered.Count == 0)
        {
            return Array.Empty<AvailabilityTimeFrameResult>();
        }

        var merged = new List<AvailabilityTimeFrameResult> { ordered[0] };
        for (var index = 1; index < ordered.Count; index++)
        {
            var current = ordered[index];
            var last = merged[^1];
            if (current.StartTime <= last.EndTime)
            {
                merged[^1] = new AvailabilityTimeFrameResult(
                    last.StartTime,
                    current.EndTime > last.EndTime ? current.EndTime : last.EndTime);
                continue;
            }

            merged.Add(current);
        }

        return merged;
    }

    private static IReadOnlyList<AvailabilityTimeFrameResult> Subtract(
        IReadOnlyList<AvailabilityTimeFrameResult> availabilityFrames,
        IReadOnlyList<AvailabilityTimeFrameResult> timeOffFrames)
    {
        if (availabilityFrames.Count == 0)
        {
            return Array.Empty<AvailabilityTimeFrameResult>();
        }

        var mergedTimeOffs = Merge(timeOffFrames);
        if (mergedTimeOffs.Count == 0)
        {
            return availabilityFrames;
        }

        var result = new List<AvailabilityTimeFrameResult>();
        foreach (var availabilityFrame in availabilityFrames)
        {
            var segmentStart = availabilityFrame.StartTime;
            foreach (var timeOffFrame in mergedTimeOffs)
            {
                if (timeOffFrame.EndTime <= segmentStart)
                {
                    continue;
                }

                if (timeOffFrame.StartTime >= availabilityFrame.EndTime)
                {
                    break;
                }

                if (timeOffFrame.StartTime > segmentStart)
                {
                    result.Add(new AvailabilityTimeFrameResult(
                        segmentStart,
                        timeOffFrame.StartTime < availabilityFrame.EndTime
                            ? timeOffFrame.StartTime
                            : availabilityFrame.EndTime));
                }

                if (timeOffFrame.EndTime >= availabilityFrame.EndTime)
                {
                    segmentStart = availabilityFrame.EndTime;
                    break;
                }

                if (timeOffFrame.EndTime > segmentStart)
                {
                    segmentStart = timeOffFrame.EndTime;
                }
            }

            if (segmentStart < availabilityFrame.EndTime)
            {
                result.Add(new AvailabilityTimeFrameResult(segmentStart, availabilityFrame.EndTime));
            }
        }

        return result;
    }

    private static int ToMinutes(TimeOnly value)
    {
        return (int)value.ToTimeSpan().TotalMinutes;
    }
}
