using Holonix.Server.Application.Interfaces;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Services;
using Xunit;

namespace Holonix.Server.Tests;

public sealed class BusinessAvailabilityCalculatorTests
{
    private readonly IBusinessAvailabilityCalculator _calculator = new BusinessAvailabilityCalculator();

    [Fact]
    public void GetLatestTimeFramesForMemberOnDate_UsesLatestEffectiveStartDateForSplitShift()
    {
        var date = new DateOnly(2026, 6, 16); // Tuesday
        var rows = new List<BusinessUserAvailability>
        {
            BuildAvailability(7, 2, "08:00", "11:00", new DateOnly(2026, 5, 1)),
            BuildAvailability(7, 2, "09:00", "12:00", new DateOnly(2026, 6, 1)),
            BuildAvailability(7, 2, "13:00", "17:30", new DateOnly(2026, 6, 1)),
        };

        var result = _calculator.GetLatestTimeFramesForMemberOnDate(7, date, rows, Array.Empty<BusinessUserTimeOff>(), new HashSet<long>());

        Assert.Collection(result,
            frame =>
            {
                Assert.Equal(new TimeOnly(9, 0), frame.StartTime);
                Assert.Equal(new TimeOnly(12, 0), frame.EndTime);
            },
            frame =>
            {
                Assert.Equal(new TimeOnly(13, 0), frame.StartTime);
                Assert.Equal(new TimeOnly(17, 30), frame.EndTime);
            });
    }

    [Fact]
    public void GetMergedTimeFramesForMembersOnDate_MergesOverlappingMemberFrames()
    {
        var date = new DateOnly(2026, 6, 16); // Tuesday
        var rows = new List<BusinessUserAvailability>
        {
            BuildAvailability(7, 2, "09:00", "12:00", new DateOnly(2026, 6, 1)),
            BuildAvailability(7, 2, "13:00", "17:30", new DateOnly(2026, 6, 1)),
            BuildAvailability(8, 2, "10:30", "14:00", new DateOnly(2026, 6, 1)),
            BuildAvailability(8, 2, "15:00", "19:00", new DateOnly(2026, 6, 1)),
        };

        var result = _calculator.GetMergedTimeFramesForMembersOnDate(
            new long[] { 7, 8 },
            date,
            rows,
            Array.Empty<BusinessUserTimeOff>(),
            new HashSet<long>());

        var frame = Assert.Single(result);
        Assert.Equal(new TimeOnly(9, 0), frame.StartTime);
        Assert.Equal(new TimeOnly(19, 0), frame.EndTime);
    }

    [Fact]
    public void GetMergedTimeFramesForMembersOnDate_ExcludesMembersOnAllDayTimeOff()
    {
        var date = new DateOnly(2026, 6, 16); // Tuesday
        var rows = new List<BusinessUserAvailability>
        {
            BuildAvailability(7, 2, "09:00", "12:00", new DateOnly(2026, 6, 1)),
            BuildAvailability(8, 2, "13:00", "17:30", new DateOnly(2026, 6, 1)),
        };

        var result = _calculator.GetMergedTimeFramesForMembersOnDate(
            new long[] { 7, 8 },
            date,
            rows,
            Array.Empty<BusinessUserTimeOff>(),
            new HashSet<long> { 8 });

        var frame = Assert.Single(result);
        Assert.Equal(new TimeOnly(9, 0), frame.StartTime);
        Assert.Equal(new TimeOnly(12, 0), frame.EndTime);
    }

    [Fact]
    public void GetMergedWeeklyTimeFramesForDay_UsesReferenceDateToSelectLatestRows()
    {
        var referenceDate = new DateOnly(2026, 6, 10);
        var rows = new List<BusinessUserAvailability>
        {
            BuildAvailability(7, 2, "08:00", "10:00", new DateOnly(2026, 5, 1)),
            BuildAvailability(7, 2, "09:00", "12:00", new DateOnly(2026, 6, 1)),
            BuildAvailability(8, 2, "13:00", "17:30", new DateOnly(2026, 6, 1)),
        };

        var result = _calculator.GetMergedWeeklyTimeFramesForDay(
            new long[] { 7, 8 },
            2,
            referenceDate,
            rows);

        Assert.Collection(result,
            frame =>
            {
                Assert.Equal(new TimeOnly(9, 0), frame.StartTime);
                Assert.Equal(new TimeOnly(12, 0), frame.EndTime);
            },
            frame =>
            {
                Assert.Equal(new TimeOnly(13, 0), frame.StartTime);
                Assert.Equal(new TimeOnly(17, 30), frame.EndTime);
            });
    }

    [Fact]
    public void GetLatestTimeFramesForMemberOnDate_SubtractsPartialDayTimeOff()
    {
        var date = new DateOnly(2026, 6, 16); // Tuesday
        var rows = new List<BusinessUserAvailability>
        {
            BuildAvailability(7, 2, "09:00", "12:00", new DateOnly(2026, 6, 1)),
            BuildAvailability(7, 2, "13:00", "17:30", new DateOnly(2026, 6, 1)),
        };
        var timeOffRows = new List<BusinessUserTimeOff>
        {
            BuildTimeOff(7, date, date, "10:00", "11:00"),
            BuildTimeOff(7, date, date, "15:00", "16:00"),
        };

        var result = _calculator.GetLatestTimeFramesForMemberOnDate(
            7,
            date,
            rows,
            timeOffRows,
            new HashSet<long>());

        Assert.Collection(result,
            frame =>
            {
                Assert.Equal(new TimeOnly(9, 0), frame.StartTime);
                Assert.Equal(new TimeOnly(10, 0), frame.EndTime);
            },
            frame =>
            {
                Assert.Equal(new TimeOnly(11, 0), frame.StartTime);
                Assert.Equal(new TimeOnly(12, 0), frame.EndTime);
            },
            frame =>
            {
                Assert.Equal(new TimeOnly(13, 0), frame.StartTime);
                Assert.Equal(new TimeOnly(15, 0), frame.EndTime);
            },
            frame =>
            {
                Assert.Equal(new TimeOnly(16, 0), frame.StartTime);
                Assert.Equal(new TimeOnly(17, 30), frame.EndTime);
            });
    }

    [Fact]
    public void GetMergedTimeFramesForMembersOnDate_SubtractsPartialDayTimeOffBeforeMerging()
    {
        var date = new DateOnly(2026, 6, 16); // Tuesday
        var rows = new List<BusinessUserAvailability>
        {
            BuildAvailability(7, 2, "09:00", "12:00", new DateOnly(2026, 6, 1)),
            BuildAvailability(8, 2, "11:00", "17:00", new DateOnly(2026, 6, 1)),
        };
        var timeOffRows = new List<BusinessUserTimeOff>
        {
            BuildTimeOff(8, date, date, "12:30", "14:00"),
        };

        var result = _calculator.GetMergedTimeFramesForMembersOnDate(
            new long[] { 7, 8 },
            date,
            rows,
            timeOffRows,
            new HashSet<long>());

        Assert.Collection(result,
            frame =>
            {
                Assert.Equal(new TimeOnly(9, 0), frame.StartTime);
                Assert.Equal(new TimeOnly(12, 30), frame.EndTime);
            },
            frame =>
            {
                Assert.Equal(new TimeOnly(14, 0), frame.StartTime);
                Assert.Equal(new TimeOnly(17, 0), frame.EndTime);
            });
    }

    [Fact]
    public void GetQualifiedTimeFramesForMembersOnDate_RequiresConfiguredHeadcount()
    {
        var date = new DateOnly(2026, 6, 16); // Tuesday
        var rows = new List<BusinessUserAvailability>
        {
            BuildAvailability(7, 2, "09:00", "12:00", new DateOnly(2026, 6, 1)),
            BuildAvailability(8, 2, "10:00", "13:00", new DateOnly(2026, 6, 1)),
            BuildAvailability(9, 2, "11:00", "14:00", new DateOnly(2026, 6, 1)),
        };

        var result = _calculator.GetQualifiedTimeFramesForMembersOnDate(
            new long[] { 7, 8, 9 },
            date,
            rows,
            Array.Empty<BusinessUserTimeOff>(),
            new HashSet<long>(),
            2);

        Assert.Collection(result,
            frame =>
            {
                Assert.Equal(new TimeOnly(10, 0), frame.StartTime);
                Assert.Equal(new TimeOnly(13, 0), frame.EndTime);
            });
    }

    private static BusinessUserAvailability BuildAvailability(
        long businessUserId,
        byte dayOfWeek,
        string startTime,
        string endTime,
        DateOnly effectiveStartDate)
    {
        return new BusinessUserAvailability
        {
            BusinessUserId = businessUserId,
            DayOfWeek = dayOfWeek,
            StartTime = TimeOnly.Parse(startTime),
            EndTime = TimeOnly.Parse(endTime),
            EffectiveStartDate = effectiveStartDate,
        };
    }

    private static BusinessUserTimeOff BuildTimeOff(
        long businessUserId,
        DateOnly effectiveStartDate,
        DateOnly effectiveEndDate,
        string startTime,
        string endTime)
    {
        return new BusinessUserTimeOff
        {
            BusinessUserId = businessUserId,
            EffectiveStartDate = effectiveStartDate,
            EffectiveEndDate = effectiveEndDate,
            EffectiveStartTime = TimeOnly.Parse(startTime),
            EffectiveEndTime = TimeOnly.Parse(endTime),
            IsAllDay = false,
        };
    }
}
