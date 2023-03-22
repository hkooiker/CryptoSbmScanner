using CryptoSbmScanner.Models;
using System.Collections.Concurrent;

namespace CryptoSbmScanner.Repositories;

public sealed class IntervalRepository
{
    public IntervalRepository()
    {
        Init();
    }

    private void Init()
    {
        _items.Clear();
        Add(Interval.Create(IntervalPeriod.OneMinute));
        Add(Interval.Create(IntervalPeriod.TwoMinutes, IntervalPeriod.OneMinute));
        Add(Interval.Create(IntervalPeriod.ThreeMinutes, IntervalPeriod.OneMinute));
        Add(Interval.Create(IntervalPeriod.FiveMinutes, IntervalPeriod.OneMinute));
        Add(Interval.Create(IntervalPeriod.TenMinutes, IntervalPeriod.FiveMinutes));
        Add(Interval.Create(IntervalPeriod.FifteenMinutes, IntervalPeriod.FiveMinutes));
        Add(Interval.Create(IntervalPeriod.ThirtyMinutes, IntervalPeriod.FifteenMinutes));
        Add(Interval.Create(IntervalPeriod.OneHour, IntervalPeriod.ThirtyMinutes));
        Add(Interval.Create(IntervalPeriod.TwoHours, IntervalPeriod.OneHour));
        Add(Interval.Create(IntervalPeriod.ThreeHours, IntervalPeriod.OneHour));
        Add(Interval.Create(IntervalPeriod.FourHours, IntervalPeriod.TwoHours));
        Add(Interval.Create(IntervalPeriod.SixHours, IntervalPeriod.ThreeHours));
        Add(Interval.Create(IntervalPeriod.EightHours, IntervalPeriod.FourHours));
        Add(Interval.Create(IntervalPeriod.TwelveHours, IntervalPeriod.SixHours));
        Add(Interval.Create(IntervalPeriod.OneDay, IntervalPeriod.TwelveHours));
    }

    private readonly ConcurrentDictionary<int, Interval> _items = new();
    private void Add(Interval interval) => _items.TryAdd((int)interval.Id, interval);
    public Interval Get(int interval) => _items.TryGetValue(interval, out Interval result) ? result : default;
}