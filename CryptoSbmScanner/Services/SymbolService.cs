using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Spot;
using CryptoSbmScanner.Models;
using CryptoSbmScanner.Repositories;
using System.Collections.Concurrent;

namespace CryptoSbmScanner.Services;

public sealed class SymbolService
{
    private readonly BinanceService _binanceService;
    private readonly SymbolRepository _symbolRepository;

    public SymbolService(BinanceService binanceService, SymbolRepository symbolRepository)
    {
        _binanceService = binanceService;
        _symbolRepository = symbolRepository;
    }

    private IEnumerable<string> _activeQuotes;

    public async Task InitAsync()
    {
        IEnumerable<BinanceSymbol> symbols = await _binanceService.GetSymbolsAsync();
        Parallel.ForEach(symbols, (symbol) => _symbolRepository.Add(Symbol.Create(new ExchangeId(Guid.NewGuid()), symbol.Name, symbol.BaseAsset, symbol.QuoteAsset)));
        InitActiveQuotes();
    }

    private void InitActiveQuotes()
    {
        _activeQuotes = _symbolRepository.All(symbol => Preferences.Default.Get($"QuoteAsset.{symbol}.FetchCandles", symbol.Quote is "BTC"))
                    .Select(symbol => symbol.Quote)
                    .Distinct();
    }

    public IEnumerable<Symbol> GetSymbols(Func<Symbol, bool> predicate = null, bool activeQuotesOnly = true)
    {
        var symbols = _symbolRepository.All();
        if (activeQuotesOnly)
        {
            symbols = symbols.Where(w => _activeQuotes.Contains(w.Quote));
        }
        if (predicate is not null)
        {
            symbols = symbols.Where(predicate);
        }
        return symbols;
    }

    public void Update(IBinanceTick tick)
    {
        Symbol symbol = _symbolRepository.Find(symbol => symbol.Name == tick.Symbol);
        symbol.Volume = tick.QuoteVolume;
    }
}