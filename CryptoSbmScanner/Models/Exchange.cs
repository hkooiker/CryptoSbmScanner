using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace CryptoSbmScanner.Models;

public sealed class Exchange
{
    private readonly ConcurrentDictionary<string, Symbol> _symbols = new();

    public Exchange(string name)
    {
        Name = name;
    }

    public string Name { get; private set; }
    public DateTimeOffset ExchangeInfoLastTime { get; set; } = DateTimeOffset.MinValue;
    public ReadOnlyDictionary<string, Symbol> Symbols => _symbols.AsReadOnly();
    public void AddSymbol(Symbol symbol)
    {
        _symbols.TryAdd(symbol.Name, symbol);
    }

    public Symbol? TryGetSymbol(string name)
    {
        _symbols.TryGetValue(name, out Symbol? symbol);
        return symbol;
    }
}