using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoSbmScanner.Models;
using CryptoSbmScanner.Services;
using System.Collections.ObjectModel;

namespace CryptoSbmScanner.ViewModels;

public partial class MainViewModel : ObservableObject
{    
    private readonly SymbolService _symbolService;
    private readonly TickerUpdateService _tickerUpdateService;
    private readonly KlineUpdateService _klineUpdateService;

    public ObservableCollection<Symbol> Symbols { get; } = new();

    public MainViewModel(SymbolService symbolService, TickerUpdateService tickerUpdateService, KlineUpdateService klineUpdateService)
    {
        _symbolService = symbolService;
        _tickerUpdateService = tickerUpdateService;
        _klineUpdateService = klineUpdateService;
    }

    [RelayCommand]
    static async Task OpenSymbolAsync(Symbol symbol)
    {
        string href = $"hypertrader://binance/{symbol.Base}-{symbol.Quote}/1".ToLowerInvariant();
        await Launcher.Default.TryOpenAsync(href);
    }

    [RelayCommand]
    void SearchSymbol(string text)
        => ShowSymbols(w => string.IsNullOrWhiteSpace(text) || w.Name.Contains(text, StringComparison.InvariantCultureIgnoreCase));

    [RelayCommand]
    async Task GetSymbolsAsync()
    {
        if (GetSymbolsCommand.IsRunning)
            return;

        await _symbolService.InitAsync();
        string[] symbolNames = _symbolService.GetSymbols().Select(s => s.Name).ToArray();
        await _tickerUpdateService.SubscribeAsync();
        await _klineUpdateService.SubscribeAsync(symbolNames);
        ShowSymbols();
    }

    void ShowSymbols(Func<Symbol,bool>? predicate = null)
    {
        Symbols.Clear();
        foreach (Symbol symbol in _symbolService.GetSymbols(predicate))
        {            
            Symbols.Add(symbol);            
        }
    }
}