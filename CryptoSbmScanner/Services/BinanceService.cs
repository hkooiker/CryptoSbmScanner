using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Objects;

namespace CryptoSbmScanner.Services;

public sealed class BinanceService
{
    private readonly IBinanceClient _binanceClient;

    public BinanceService(IBinanceClient binanceClient)
    {
        _binanceClient = binanceClient;
    }

    public async Task<IEnumerable<BinanceSymbol>> GetSymbolsAsync()
    {
        WebCallResult<BinanceExchangeInfo> info = await _binanceClient.SpotApi.ExchangeData.GetExchangeInfoAsync();
        return info.Success
            ? info.Data.Symbols.Where(w => w.Status == SymbolStatus.Trading || w.Status == SymbolStatus.EndOfDay).Where(w => w.IsSpotTradingAllowed)
            : Enumerable.Empty<BinanceSymbol>();
    }
}