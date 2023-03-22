using CryptoSbmScanner.Models;
using System.Collections.Concurrent;

namespace CryptoSbmScanner.Repositories;

public sealed class SymbolRepository
{
    private readonly ConcurrentDictionary<SymbolId, Symbol> _items = new();

    public void Add(Symbol symbol) => _items.TryAdd(symbol.Id, symbol);
    public Symbol Get(SymbolId id) => _items.TryGetValue(id, out Symbol symbol) ? symbol : default;
    public Symbol Find(Func<Symbol, bool> predicate) => _items.Values.FirstOrDefault(predicate);
    public IEnumerable<Symbol> All(Func<Symbol, bool> predicate = null) => predicate is null ? _items.Values : _items.Values.Where(predicate);
}