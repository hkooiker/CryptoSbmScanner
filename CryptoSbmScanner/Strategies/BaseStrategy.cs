using CryptoSbmScanner.Models;
using Skender.Stock.Indicators;
using Indicator = CryptoSbmScanner.Models.Indicator;

namespace CryptoSbmScanner.Strategies;

public abstract class BaseStrategy
{
    protected Interval Interval;

    public BaseStrategy(Interval interval)
    {
        Interval = interval;
    }

    public abstract bool IsSignal();
    public abstract bool GiveUp();
    public abstract bool AllowStepIn();
    public bool CheckBaseSignals()
    {
        IEnumerable<Candle> candles = Interval.Candles.OrderBy(candle => candle.Key)
                                      .TakeLast(60)
                                      .Select(s => s.Value);
        return !HasFlatPrices(candles)
            && !HasZeroVolumes(candles)
            && !HasBeenAboveSma20(candles)
            && !HasBeenAboveBollingerBands(candles);
    }

    private static bool HasFlatPrices(IEnumerable<Candle> candles, int times = 16)
        => candles.Count(c => c.Close == c.Open && c.Close == c.High && c.Close == c.Low) > times;

    private static bool HasZeroVolumes(IEnumerable<Candle> candles, int times = 18)
        => candles.Count(c => c.Volume == 0) > times;

    private static bool HasBeenAboveSma20(IEnumerable<Candle> candles, int times = 2)
    {
        int count = 0;
        _ = candles.Aggregate((a, b) =>
        {
            a.Indicators.TryGetValue(Indicator.Sma20, out IReusableResult? sma20resultA);
            b.Indicators.TryGetValue(Indicator.Sma20, out IReusableResult? sma20resultB);
            if (sma20resultA is SmaResult sma20a
            && sma20resultB is SmaResult sma20b
            && sma20a.Sma.HasValue
            && sma20b.Sma.HasValue
            && Math.Max(b.Open, b.Close) >= (decimal)sma20b.Sma.Value
            && Math.Max(a.Open, a.Close) < (decimal)sma20a.Sma.Value)
            {
                count++;
            }
            return b;
        });
        return count > times;
    }

    private static bool HasBeenAboveBollingerBands(IEnumerable<Candle> candles, int times = 2)
    {
        int count = 0;
        _ = candles.Aggregate((a, b) =>
        {
            a.Indicators.TryGetValue(Indicator.BollingerBands, out IReusableResult? bollingerBandsResultA);
            b.Indicators.TryGetValue(Indicator.BollingerBands, out IReusableResult? bollingerBandsResultB);
            if (bollingerBandsResultA is BollingerBandsResult bollingerBandsA
            && bollingerBandsResultB is BollingerBandsResult bollingerBandsB
            && bollingerBandsA.UpperBand.HasValue
            && bollingerBandsB.UpperBand.HasValue
            && Math.Max(b.Open, b.Close) >= (decimal)bollingerBandsB.UpperBand.Value
            && Math.Max(a.Open, a.Close) < (decimal)bollingerBandsA.UpperBand.Value)
            {
                count++;
            }
            return b;
        });
        return count > times;
    }
}