using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Spot;
using CryptoSbmScanner.Models;
using CryptoSbmScanner.Repositories;

namespace CryptoSbmScanner.Services;

public sealed class SymbolService
{
    private readonly BinanceService _binanceService;
    private readonly ExchangeRepository _exchangeRepository;

    public SymbolService(BinanceService binanceService, ExchangeRepository exchangeRepository)
    {
        _binanceService = binanceService;
        _exchangeRepository = exchangeRepository;
    }

    private IEnumerable<string> _activeQuotes = Enumerable.Empty<string>();

    public async Task InitAsync()
    {
        IEnumerable<BinanceSymbol> symbols = await _binanceService.GetSymbolsAsync();
        Parallel.ForEach(symbols.Take(1), (symbol) => _exchangeRepository
            .TryGet("Binance")?
            .AddSymbol(new Symbol(symbol.Name, symbol.BaseAsset, symbol.QuoteAsset)));
        InitActiveQuotes();
    }

    private void InitActiveQuotes()
    {
        IEnumerable<string>? activeQuotes = _exchangeRepository
            .TryGet("Binance")?
            .Symbols
            .Where(symbol => Preferences.Default.Get($"QuoteAsset.{symbol.Value.Quote}.FetchCandles", symbol.Value.Quote is "BTC"))
            .Select(symbol => symbol.Value.Quote)
            .Distinct();
        if(activeQuotes is not null)
        {
            _activeQuotes = activeQuotes;
        }
    }

    public IEnumerable<Symbol> GetSymbols(Func<Symbol, bool>? predicate = null, bool activeQuotesOnly = true)
    {
        IEnumerable<Symbol>? symbols = _exchangeRepository
            .TryGet("Binance")?
            .Symbols
            .Select(s => s.Value);
        if (symbols is not null)
        {
            if (predicate is not null)
            {
                symbols = symbols.Where(predicate);
            }
            if (activeQuotesOnly)
            {
                symbols = symbols.Where(w => _activeQuotes.Contains(w.Quote));
            }
        }
        return symbols ?? Enumerable.Empty<Symbol>();
    }

    public void Update(IBinanceTick tick)
    {
        Symbol? symbol = _exchangeRepository
            .TryGet("Binance")?            
            .TryGetSymbol(tick.Symbol);

        if (symbol is null)
            return;

        symbol.Volume = tick.QuoteVolume;
    }
}