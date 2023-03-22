using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoSbmScanner.Models;
using CryptoSbmScanner.Notifications;
using CryptoSbmScanner.Services;
using MediatR;
using System.Collections.ObjectModel;

namespace CryptoSbmScanner.ViewModels;

public partial class MainViewModel : ObservableObject
{    
    private readonly SymbolService _symbolService;
    private readonly IPublisher _publisher;

    public ObservableCollection<Symbol> Symbols { get; } = new();

    public MainViewModel(SymbolService symbolService, IPublisher mediator)
    {
        _symbolService = symbolService;
        _publisher = mediator;
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
        await _publisher.Publish(new SymbolsLoaded(_symbolService.GetSymbols().Select(s => s.Name).ToArray()));
        ShowSymbols();
    }

    void ShowSymbols(Func<Symbol,bool> predicate = null)
    {
        Symbols.Clear();
        foreach (Symbol symbol in _symbolService.GetSymbols(predicate))
        {            
            Symbols.Add(symbol);            
        }
    }
}