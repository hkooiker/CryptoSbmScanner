using Binance.Net.Enums;

namespace CryptoSbmScanner.Extensions;

public static class IntervalExtensions
{
    public static KlineInterval[] Constructing(this KlineInterval[] intervals)
        => intervals.Where(interval => intervals.Any(w => (int)w > (int)interval && (int)w % (int)interval is 0) || interval is KlineInterval.OneMinute).ToArray();
}
