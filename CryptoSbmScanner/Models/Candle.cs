using Skender.Stock.Indicators;

namespace CryptoSbmScanner.Models;

public sealed class Candle : IQuote
{
    public Candle(decimal open, decimal high, decimal low, decimal close, decimal volume, DateTime date)
    {
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
        Date = date;
    }

    public decimal Open { get; private set; }

    public decimal High { get; private set; }

    public decimal Low { get; private set; }

    public decimal Close { get; private set; }

    public decimal Volume { get; private set; }

    public DateTime Date { get; private set; }
}