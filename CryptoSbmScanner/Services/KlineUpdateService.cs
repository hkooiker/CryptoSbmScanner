using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using CryptoSbmScanner.Models;
using CryptoSbmScanner.Notifications;
using CryptoSbmScanner.Repositories;
using MediatR;

namespace CryptoSbmScanner.Services;

public sealed class KlineUpdateService : INotificationHandler<SymbolsLoaded>
{
    private readonly IBinanceClient _binanceClient;
    private readonly IBinanceSocketClient _binanceSocketClient;
    private readonly CandleRepository _candleRepository;
    private readonly SymbolRepository _symbolRepository;

    private UpdateSubscription _subscription;
    private readonly KlineInterval[] _intervals = Enum.GetValues(typeof(KlineInterval)).Cast<KlineInterval>().Where(w => (int)w is >= 60 and <= 86400).ToArray();

    public KlineUpdateService(IBinanceSocketClient socketClient, IBinanceClient binanceClient, CandleRepository candleRepository, SymbolRepository symbolRepository)
    {
        _binanceSocketClient = socketClient;
        _binanceClient = binanceClient;
        _candleRepository = candleRepository;
        _symbolRepository = symbolRepository;
    }

    public async Task Handle(SymbolsLoaded notification, CancellationToken cancellationToken)
    {
        var symbols = _symbolRepository.All(symbol => notification.Symbols.Contains(symbol.Name));
        CallResult<UpdateSubscription> subscription = await _binanceSocketClient.SpotStreams.SubscribeToKlineUpdatesAsync(notification.Symbols, KlineInterval.OneMinute, (message) =>
        {
            var kline = message.Data.Data;
            if (kline.Final)
            {
                var symbol = symbols.Single(f => f.Name == message.Data.Symbol);
                _candleRepository.Add(Candle.Create(kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.OpenTime, symbol.Id, (IntervalPeriod)kline.Interval));
            }
        }, cancellationToken);
        if (subscription.Success)
        {
            _subscription = subscription.Data;
            await Task.WhenAll(symbols.SelectMany(s => _intervals.Select(i => new { Symbol = s, Interval = i })).Select(s => GetKlines(s.Symbol, s.Interval, cancellationToken)));
        }
    }

    private async Task GetKlines(Symbol symbol, KlineInterval interval, CancellationToken cancellationToken)
    {
        DateTime endTime = DateTime.Now;
        DateTime startTime = endTime.AddSeconds(-(215 * (int)interval));
        int limit = 1000;
        while (limit == 1000)
        {
            if (await _binanceClient.SpotApi.ExchangeData.GetKlinesAsync(symbol.Name, interval, startTime, endTime, limit, cancellationToken) is WebCallResult<IEnumerable<IBinanceKline>> klinesResult && klinesResult.Success)
            {
                Parallel.ForEach(klinesResult.Data, (kline) => _candleRepository.Add(Candle.Create(kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.OpenTime, symbol.Id, (IntervalPeriod)interval)));
                startTime = klinesResult.Data.Last().OpenTime;
                limit = klinesResult.Data.Count();
            }
        }
    }
}