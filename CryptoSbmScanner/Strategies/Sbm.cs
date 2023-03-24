using CryptoSbmScanner.Extensions;
using CryptoSbmScanner.Models;

namespace CryptoSbmScanner.Strategies;

public sealed class Sbm : BaseStrategy
{
    public Sbm(Interval interval) : base(interval)
    {
        
    }

    public override bool AllowStepIn()
    {
        return false;
    }

    public override bool GiveUp()
    {
        return false;
    }

    public override bool IsSignal() 
        => Interval.Candles.MaxBy(c => c.Key).Value is Candle candle
            && candle.CheckBollingerBandsWidth(1.5)
            && candle.CheckSbmConditions()
            && candle.IsBelowBollingerBands()
            && candle.IsStochOversold();
}