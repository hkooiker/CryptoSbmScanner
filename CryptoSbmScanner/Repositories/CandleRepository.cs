using CryptoSbmScanner.Models;
using System.Collections.Concurrent;

namespace CryptoSbmScanner.Repositories;

public sealed class CandleRepository
{
    private readonly ConcurrentDictionary<string, Candle> _items = new();

    public void Add(Candle candle) => _items.AddOrUpdate(candle.Id, candle, (_,_) => candle);

}
