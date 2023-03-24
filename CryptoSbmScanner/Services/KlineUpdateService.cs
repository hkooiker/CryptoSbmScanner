using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using CryptoSbmScanner.Extensions;
using CryptoSbmScanner.Models;
using CryptoSbmScanner.Repositories;

namespace CryptoSbmScanner.Services;

public sealed class KlineUpdateService
{
    private readonly IBinanceClient _binanceClient;
    private readonly IBinanceSocketClient _binanceSocketClient;
    private readonly ExchangeRepository _exchangeRepository;

    private UpdateSubscription? _subscription;
    private readonly IntervalPeriod[] _intervals = IntervalPeriodExtensions.ConstructingIntervals.Take(1).ToArray();

    public KlineUpdateService(IBinanceSocketClient socketClient, IBinanceClient binanceClient, ExchangeRepository exchangeRepository)
    {
        _binanceSocketClient = socketClient;
        _binanceClient = binanceClient;
        _exchangeRepository = exchangeRepository;
    }

    public async Task SubscribeAsync(string[] symbolNames, CancellationToken cancellationToken = default)
    {
        IEnumerable<Symbol>? symbols = _exchangeRepository
            .TryGet("Binance")?
            .Symbols
            .Where(symbol => symbolNames.Contains(symbol.Key))
            .Select(s => s.Value);
        if (symbols is not null)
        {
            CallResult<UpdateSubscription> subscription = await _binanceSocketClient.SpotStreams.SubscribeToKlineUpdatesAsync(symbolNames, KlineInterval.OneMinute, (message) =>
            {
                IBinanceStreamKline kline = message.Data.Data;
                if (kline.Final)
                {
                    Symbol symbol = symbols.Single(f => f.Name == message.Data.Symbol);
                    Interval interval = symbol.Intervals[(IntervalPeriod)kline.Interval];
                    interval.AddCandle(new Candle(kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.OpenTime));
                }
            }, cancellationToken);
            if (subscription.Success)
            {
                _subscription = subscription.Data;
                await Task.WhenAll(symbols.SelectMany(s => _intervals.Select(i => new { Symbol = s, Interval = i })).Select(s => GetKlines(s.Symbol, s.Interval, cancellationToken)));
            }
        }
    }

    private async Task GetKlines(Symbol symbol, IntervalPeriod interval, CancellationToken cancellationToken)
    {
        Interval symbolInterval = symbol.Intervals[interval];
        DateTime endTime = DateTime.Now;
        DateTime startTime = endTime.AddSeconds(-(215 * (int)interval));
        int limit = 1000;
        while (limit == 1000)
        {
            if (await _binanceClient.SpotApi.ExchangeData.GetKlinesAsync(symbol.Name, (KlineInterval)interval, startTime, endTime, limit, cancellationToken) is WebCallResult<IEnumerable<IBinanceKline>> klinesResult && klinesResult.Success)
            {
                Parallel.ForEach(klinesResult.Data, (kline)
                    => symbolInterval.AddCandle(new Candle(kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.OpenTime)));
                
                startTime = klinesResult.Data.Last().OpenTime;
                limit = klinesResult.Data.Count();
            }
        }

        CalculateConstructableIntervals(symbol, interval);
    }

    private static void CalculateConstructableIntervals(Symbol symbol, IntervalPeriod intervalPeriod)
    {
        Interval interval = symbol.Intervals[intervalPeriod];
        foreach (IntervalPeriod constructableInterval in interval.Id.ConstructableIntervals())
        {
            Interval symbolInterval = symbol.Intervals[constructableInterval];
            TimeSpan period = TimeSpan.FromSeconds((int)constructableInterval);
            interval.Candles.Values.AsParallel()
                .Select(candle => new
                {
                    candle.Date,
                    candle.Low,
                    candle.Open,
                    candle.Close,
                    candle.High,
                    candle.Volume,
                    Period = candle.Date.Ticks / period.Ticks
                })
                .GroupBy(g => g.Period)
                .Select(group => new Candle(
                    open: group.OrderBy(s => s.Date).First().Open,
                    high: group.Max(d => d.High),
                    low: group.Min(d => d.Low),
                    close: group.OrderBy(s => s.Date).Last().Close,
                    volume: group.Sum(s => s.Volume),
                    date: new DateTime(period.Ticks * group.Min(d => d.Period))
                ))
                .ForAll(symbolInterval.AddCandle);
        }
    }
}