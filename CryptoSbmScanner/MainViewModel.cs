using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Spot;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoExchange.Net.Objects;
using System.Collections.ObjectModel;

namespace CryptoSbmScanner;

public partial class MainViewModel : ObservableObject
{
    private readonly IBinanceClient _binanceClient;
    private IOrderedEnumerable<BinanceSymbol> _symbols;

    public ObservableCollection<BinanceSymbol> Symbols { get; } = new();

    public MainViewModel(IBinanceClient binanceClient)
    {
        _binanceClient = binanceClient;
    }

    [RelayCommand]
    static async Task OpenSymbolAsync(BinanceSymbol symbol)
    {
        string href = $"hypertrader://binance/{symbol.BaseAsset}-{symbol.QuoteAsset}/1".ToLowerInvariant();
        await Launcher.Default.TryOpenAsync(href);
    }

    [RelayCommand]
    void SearchSymbol(TextChangedEventArgs args) 
        => AddSymbols(_symbols.Where(w => string.IsNullOrWhiteSpace(args.NewTextValue) || w.Name.Contains(args.NewTextValue, StringComparison.InvariantCultureIgnoreCase)));

    [RelayCommand]
    async Task GetSymbolsAsync()
    {
        if (GetSymbolsCommand.IsRunning)
            return;

        WebCallResult<BinanceExchangeInfo> info = await _binanceClient.SpotApi.ExchangeData.GetExchangeInfoAsync();
        if (!info.Success)
        {
            return;
        }
        _symbols = info.Data.Symbols.OrderBy(o => o.Name);
        AddSymbols(_symbols);
    }

    void AddSymbols(IEnumerable<BinanceSymbol> symbols)
    {
        Symbols.Clear();
        foreach (BinanceSymbol symbol in symbols)
        {
            Symbols.Add(symbol);
        }
    }
}