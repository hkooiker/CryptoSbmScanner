using CryptoSbmScanner.Models;

namespace CryptoSbmScanner.Extensions;

public static class IntervalPeriodExtensions
{
    private static readonly Dictionary<IntervalPeriod, IntervalPeriod?> _intervals = new()
    {
        { IntervalPeriod.OneMinute, null },
        { IntervalPeriod.TwoMinutes, IntervalPeriod.OneMinute },
        { IntervalPeriod.ThreeMinutes, IntervalPeriod.OneMinute },
        { IntervalPeriod.FiveMinutes, IntervalPeriod.OneMinute },
        { IntervalPeriod.TenMinutes, IntervalPeriod.FiveMinutes },
        { IntervalPeriod.FifteenMinutes, IntervalPeriod.FiveMinutes },
        { IntervalPeriod.ThirtyMinutes, IntervalPeriod.FifteenMinutes },
        { IntervalPeriod.OneHour, IntervalPeriod.ThirtyMinutes },
        { IntervalPeriod.TwoHours, IntervalPeriod.OneHour },
        { IntervalPeriod.ThreeHours, IntervalPeriod.OneHour },
        { IntervalPeriod.FourHours, IntervalPeriod.TwoHours },
        { IntervalPeriod.SixHours, IntervalPeriod.ThreeHours },
        { IntervalPeriod.EightHours, IntervalPeriod.FourHours },
        { IntervalPeriod.TwelveHours, IntervalPeriod.SixHours },
        { IntervalPeriod.OneDay, IntervalPeriod.TwelveHours }
    };

    public static IntervalPeriod[] ConstructingIntervals
        => _intervals.Values.Where(w => w.HasValue)
                            .Select(s => s!.Value)
                            .Distinct()
                            .ToArray();

    public static IntervalPeriod[] ConstructableIntervals(this IntervalPeriod interval)
        => _intervals.Where(w => w.Value == interval)
                     .Select(s => s.Key)
                     .ToArray();
}
