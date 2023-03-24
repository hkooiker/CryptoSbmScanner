using CryptoSbmScanner.Models;
using Skender.Stock.Indicators;
using Indicator = CryptoSbmScanner.Models.Indicator;

namespace CryptoSbmScanner.Extensions;

public static class CandleExtensions
{
    public static double BollingerBandsWidth(this Candle candle) 
        => candle.Indicators.TryGetValue(Indicator.BollingerBands, out IReusableResult? result)
            && result is BollingerBandsResult bollingerBands
            && bollingerBands.UpperBand.HasValue
            && bollingerBands.LowerBand.HasValue
            ? 100 * ((bollingerBands.UpperBand.Value / bollingerBands.LowerBand.Value) - 1)
            : 0;

    public static bool CheckBollingerBandsWidth(this Candle candle, double min, double? max = null)
    {
        double bbPercentage = candle.BollingerBandsWidth();
        return min > 0 && (max ?? double.MaxValue) > min && bbPercentage > min && bbPercentage < max;
    }

    public static bool CheckSbmConditions(this Candle candle) 
        => candle.Indicators.TryGetValue(Indicator.Sma20, out IReusableResult? sma20result)
            && candle.Indicators.TryGetValue(Indicator.Sma50, out IReusableResult? sma50result)
            && candle.Indicators.TryGetValue(Indicator.Sma50, out IReusableResult? sma200result)
            && candle.Indicators.TryGetValue(Indicator.Psar, out IReusableResult? psarResult)
            && sma20result is SmaResult sma20
            && sma50result is SmaResult sma50
            && sma200result is SmaResult sma200
            && psarResult is ParabolicSarResult psar
            && psar.Sar.HasValue
            && sma50.Sma < sma200.Sma
            && sma20.Sma < sma200.Sma
            && sma20.Sma < sma50.Sma
            && psar.Sar <= sma20.Sma
            && (decimal)psar.Sar.Value > candle.Close
            && CheckSmaPercentage(sma200.Sma.Value, sma50.Sma.Value, 0.3m)
            && CheckSmaPercentage(sma200.Sma.Value, sma20.Sma.Value, 0.7m)
            && CheckSmaPercentage(sma50.Sma.Value, sma20.Sma.Value, 0.5m);

    public static bool IsBelowBollingerBands(this Candle candle, bool useLow = false) 
        => candle.Indicators.TryGetValue(Indicator.BollingerBands, out IReusableResult? bollingerBandsResult)
            && bollingerBandsResult is BollingerBandsResult bollingerBands
            && bollingerBands.LowerBand.HasValue
            && (useLow ? candle.Low : Math.Min(candle.Open, candle.Close)) <= (decimal)bollingerBands.LowerBand;

    public static bool IsStochOversold(this Candle candle) => candle.Indicators.TryGetValue(Indicator.Stoch, out IReusableResult? stochResult)
            && stochResult is StochResult stoch
            && stoch.Signal <= 20
            && stoch.Oscillator <= 20;

    private static bool CheckSmaPercentage(double sma1, double sma2, decimal v) 
        => 100 * (decimal)sma1 - (decimal)sma2 / ((decimal)sma1 + (decimal)sma2) / 2 >= v;
}