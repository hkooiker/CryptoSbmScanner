using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using CryptoSbmScanner.Extensions;
using CryptoSbmScanner.Models;
using CryptoSbmScanner.Repositories;
using CryptoSbmScanner.Strategies;
using Skender.Stock.Indicators;

namespace CryptoSbmScanner.Services;

public sealed class KlineUpdateService
{
    private readonly IBinanceClient _binanceClient;
    private readonly IBinanceSocketClient _binanceSocketClient;
    private readonly ExchangeRepository _exchangeRepository;

    private readonly IntervalPeriod[] _intervals = IntervalPeriodExtensions.ConstructingIntervals.ToArray();
    private readonly IEnumerable<Type> _strategies = AppDomain.CurrentDomain.GetAssemblies()
                                                                               .SelectMany(assemly => assemly.GetTypes())
                                                                               .Where(type => type.IsSubclassOf(typeof(BaseStrategy)));
    private UpdateSubscription? _subscription;


    public KlineUpdateService(IBinanceSocketClient socketClient, IBinanceClient binanceClient, ExchangeRepository exchangeRepository)
    {
        _binanceSocketClient = socketClient;
        _binanceClient = binanceClient;
        _exchangeRepository = exchangeRepository;
    }

    public async Task SubscribeAsync(string[] symbolNames, CancellationToken cancellationToken = default)
    {
        CallResult<UpdateSubscription> subscription = await _binanceSocketClient.SpotStreams.SubscribeToKlineUpdatesAsync(symbolNames, KlineInterval.OneMinute, KlineUpdate, cancellationToken);
        if (subscription.Success)
        {
            _subscription = subscription.Data;
            await Parallel.ForEachAsync(symbolNames.SelectMany(s => _intervals.Select(i => new { Symbol = s, Interval = i })), async (a, token) =>
            {
                await GetKlinesAsync(a.Symbol, a.Interval, token);
            });
        }
    }

    private void KlineUpdate(DataEvent<IBinanceStreamKlineData> message)
    {
        IBinanceStreamKline kline = message.Data.Data;
        if (kline.Final && _exchangeRepository.TryGet("Binance")?.TryGetSymbol(message.Data.Symbol) is Symbol symbol)
        {
            Interval interval = symbol.Intervals[(IntervalPeriod)kline.Interval];
            interval.AddCandle(new Candle(kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.OpenTime));
            CalculateIndicators(interval);
            AnalyseStrategies(interval);
        }
    }

    private void AnalyseStrategies(Interval interval)
    {
        foreach (object? strategy in _strategies.Select(type => Activator.CreateInstance(type, interval)))
        {
            if (strategy is BaseStrategy baseStrategy
                && baseStrategy.IsSignal()
                && baseStrategy.CheckBaseSignals())
            {

            }
        }
    }

    private static void CalculateIndicators(Interval interval)
    {
        Candle candle = interval.Candles.MaxBy(c => c.Key).Value;
        IEnumerable<Candle> candles = interval.Candles.Values.Validate();
        candle.AddIndicatorResult(Models.Indicator.BollingerBands, candles.GetBollingerBands().Last());
        candle.AddIndicatorResult(Models.Indicator.Sma20, candles.GetSma(20).Last());
        candle.AddIndicatorResult(Models.Indicator.Sma50, candles.GetSma(50).Last());
        candle.AddIndicatorResult(Models.Indicator.Sma200, candles.GetSma(200).Last());
        candle.AddIndicatorResult(Models.Indicator.Rsi, candles.GetRsi().Last());
        candle.AddIndicatorResult(Models.Indicator.Macd, candles.GetMacd().Last());
        candle.AddIndicatorResult(Models.Indicator.Stoch, candles.GetStoch(14, 3, 1).Last());
        candle.AddIndicatorResult(Models.Indicator.Psar, candles.GetParabolicSar().Last());
    }

    private async Task GetKlinesAsync(string symbolName, IntervalPeriod interval, CancellationToken cancellationToken)
    {
        if (_exchangeRepository.TryGet("Binance")?.TryGetSymbol(symbolName) is Symbol symbol 
            && symbol.Intervals.TryGetValue(interval, out Interval? symbolInterval))
        {
            DateTime endTime = DateTime.Now;
            DateTime startTime = endTime.AddSeconds(-(215 * (int)interval));
            int limit = 1000;
            while (limit == 1000)
            {
                if (await _binanceClient.SpotApi.ExchangeData.GetKlinesAsync(symbol.Name, (KlineInterval)interval, startTime, endTime, limit, cancellationToken) is WebCallResult<IEnumerable<IBinanceKline>> klinesResult && klinesResult.Success)
                {
                    klinesResult.Data.AsParallel()
                        .Select(kline => new Candle(kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.OpenTime))
                        .ForAll(symbolInterval.AddCandle);
                    startTime = klinesResult.Data.Last().OpenTime;
                    limit = klinesResult.Data.Count();
                }
            }
            CalculateConstructableIntervals(symbol, interval);
        }
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
                    group.OrderBy(s => s.Date).First().Open,
                    group.Max(d => d.High),
                    group.Min(d => d.Low),
                    group.OrderBy(s => s.Date).Last().Close,
                    group.Sum(s => s.Volume),
                    new DateTime(period.Ticks * group.Min(d => d.Period))
                ))
                .ForAll(symbolInterval.AddCandle);
        }
    }
}