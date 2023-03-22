using Skender.Stock.Indicators;

namespace CryptoSbmScanner.Models;

public sealed class Candle : IQuote
{
    private Candle() { }
    public string Id => $"{SymbolId}_{Interval}_{Date}";

    public decimal Open { get; private set; }

    public decimal High { get; private set; }

    public decimal Low { get; private set; }

    public decimal Close { get; private set; }

    public decimal Volume { get; private set; }

    public DateTime Date { get; private set; }
    public SymbolId SymbolId { get; private set; }
    public IntervalPeriod Interval { get; private set; }
    public static Candle Create(decimal open, decimal high, decimal low, decimal close, decimal volume, DateTime date, SymbolId symbolId, IntervalPeriod interval) => new()
    {
        Open = open,
        High = high,
        Low = low,
        Close = close,
        Volume = volume,
        Date = date,
        SymbolId = symbolId,
        Interval = interval
    };
}