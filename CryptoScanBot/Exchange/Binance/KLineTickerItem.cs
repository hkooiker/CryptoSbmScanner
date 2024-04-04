﻿using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;

using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.Binance;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
/// </summary>
public class KLineTickerItem(string apiExchangeName, CryptoQuoteData quoteData) : KLineTickerItemBase(apiExchangeName, quoteData)
{
    private BinanceSocketClient socketClient;
    private UpdateSubscription _subscription;

    private void ProcessCandle(BinanceStreamKlineData kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(kline.Symbol, out CryptoSymbol symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", temp.Symbol, temp.Data.OpenTime.ToLocalTime()));
                Process1mCandle(symbol, kline.Data.OpenTime, kline.Data.OpenPrice, kline.Data.HighPrice, kline.Data.LowPrice, kline.Data.ClosePrice, kline.Data.Volume);

            }
        }

    }


    public override async Task StartAsync()
    {
        if (_subscription != null)
        {
            ScannerLog.Logger.Trace($"kline ticker for group {GroupName} already started");
            return;
        }


        ConnectionLostCount = 0;
        ScannerLog.Logger.Trace($"kline ticker for group {GroupName} starting");

        if (Symbols.Count > 0)
        {
            socketClient = new BinanceSocketClient();
            CallResult<UpdateSubscription> subscriptionResult = await socketClient.SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(
                Symbols, KlineInterval.OneMinute, (data) =>
            {
                if (data.Data.Data.Final)
                {
                    Task.Run(() => { ProcessCandle(data.Data as BinanceStreamKlineData); });
                }
            }, ExchangeHelper.CancellationToken).ConfigureAwait(false);


            // Subscribe to network-related stuff
            if (subscriptionResult.Success)
            {
                ErrorDuringStartup = false;
                _subscription = subscriptionResult.Data;

                // Events
                _subscription.Exception += Exception;
                _subscription.ConnectionLost += ConnectionLost;
                _subscription.ConnectionRestored += ConnectionRestored;


                //    // TODO: Put a CancellationToken in order to stop it gracefully
                //    BinanceClient client = new();
                //    var keepAliveTask = Task.Run(async () =>
                //    {
                //        while (true)
                //        {
                //            await client.SpotApi.Account.KeepAliveUserStreamAsync(subscriptionResult.Data.); //???
                //            await Task.Delay(TimeSpan.FromMinutes(30));
                //        }
                //    });
                //GlobalData.AddTextToLogTab($"{Api.ExchangeName} {quote} 1m started candle stream {symbols.Count} symbols");
                ScannerLog.Logger.Trace($"kline ticker for group {GroupName} started");
            }
            else
            {
                _subscription = null;
                socketClient.Dispose();
                socketClient = null;
                ConnectionLostCount++;
                ErrorDuringStartup = true;
                ScannerLog.Logger.Trace($"kline ticker for group {GroupName} error {subscriptionResult.Error.Message} {string.Join(',', Symbols)}");
                GlobalData.AddTextToLogTab($"kline ticker for group {GroupName} error {subscriptionResult.Error.Message} {string.Join(',', Symbols)}");
            }
        }
    }

    public override async Task StopAsync()
    {
        if (_subscription == null)
        {
            ScannerLog.Logger.Trace($"kline ticker for group {GroupName} already stopped");
            return;
        }

        ScannerLog.Logger.Trace($"kline ticker for group {GroupName} stopping");
        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient?.UnsubscribeAsync(_subscription);
        _subscription = null;

        socketClient?.Dispose();
        socketClient = null;
        ScannerLog.Logger.Trace($"kline ticker for group {GroupName} stopped");
    }

    private void ConnectionLost()
    {
        ConnectionLostCount++;
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteData.Name} kline ticker for group {GroupName} connection lost.");
        ScannerSession.ConnectionWasLost("");
    }

    private void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {QuoteData.Name} kline ticker for group {GroupName} connection restored.");
        ScannerSession.ConnectionWasRestored("");
    }

    private void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} kline ticker for group {GroupName} connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}
