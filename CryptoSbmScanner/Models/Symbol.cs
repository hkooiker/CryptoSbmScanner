using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace CryptoSbmScanner.Models;

public sealed class Symbol
{
    private readonly ConcurrentDictionary<IntervalPeriod, Interval> _intervals = new();

    public Symbol(string name, string @base, string quote)
    {
        Name = name;
        Base = @base;
        Quote = quote;

        foreach(IntervalPeriod interval in Enum.GetValues(typeof(IntervalPeriod)))
        {
            Add(new Interval(interval));
        }
    }
    
    public string Name { get; private set; }
    public string Base { get; private set; }
    public string Quote { get; private set; }
    public decimal Volume { get; set; }
    public ReadOnlyDictionary<IntervalPeriod, Interval> Intervals => _intervals.AsReadOnly();
    private void Add(Interval interval) => _intervals.TryAdd(interval.Id, interval);
}