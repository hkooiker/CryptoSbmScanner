using CryptoSbmScanner.Models;
using System.Collections.Concurrent;

namespace CryptoSbmScanner.Repositories;

public sealed class ExchangeRepository
{
    private readonly ConcurrentDictionary<ExchangeId, Exchange> _items = new();

    public ExchangeRepository()
    {        
        Add(Exchange.Create("Binance"));
    }

    public void Add(Exchange exchange) => _items.TryAdd(exchange.Id, exchange);

    public Exchange Get(ExchangeId id) => _items.TryGetValue(id, out Exchange exchange) ? exchange : default;

    public Exchange Find(Func<Exchange, bool> predicate) => _items.Values.FirstOrDefault(predicate);
}