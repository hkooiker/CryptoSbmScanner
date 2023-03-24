using CryptoSbmScanner.Models;
using System.Collections.Concurrent;

namespace CryptoSbmScanner.Repositories;

public sealed class ExchangeRepository
{
    private readonly ConcurrentDictionary<string, Exchange> _items = new();

    public ExchangeRepository()
    {
        Add(new Exchange("Binance"));
    }

    private void Add(Exchange exchange)
        => _items.TryAdd(exchange.Name!, exchange);

    public Exchange? TryGet(string name)
    {
        _items.TryGetValue(name, out Exchange? exchange);
        return exchange;
    }
}