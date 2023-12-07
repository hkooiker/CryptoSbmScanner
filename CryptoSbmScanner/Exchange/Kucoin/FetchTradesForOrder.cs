﻿using Kucoin.Net.Clients;
using Kucoin.Net.Enums;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Exchange.Kucoin;

#if TRADEBOT
public class FetchTradeForOrder
{
    public static async Task<int> FetchTradesForOrderAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, string orderId)
    {
        int tradeCount = 0;
        try
        {
            // TODO
            using KucoinRestClient client = new();
            var result = await client.SpotApi.Trading.GetUserTradesAsync(symbol.Name, orderId: orderId);
            if (!result.Success)
            {
                GlobalData.AddTextToLogTab($"FetchTradesForOrderAsync: error getting trades order {orderId} {result.Error}");
            }

            List<CryptoTrade> tradeCache = new();
            if (result.Data != null)
            {
                foreach (var item in result.Data.Items)
                {
                    if (!symbol.TradeList.TryGetValue(item.Id, out CryptoTrade trade))
                    {
                        //trade = new CryptoTrade();
                        //Api.PickupTrade(tradeAccount, symbol, trade, item);
                        //tradeCache.Add(trade);
                        //GlobalData.AddTrade(trade);
                    }
                }
            }
            tradeCount = tradeCache.Count;


            // Verwerk de trades
            if (tradeAccount.Id > 0)
            {
                using CryptoDatabase databaseThread = new();
                {
                    databaseThread.Open();
                    Monitor.Enter(symbol.TradeList);
                    try
                    {
                        using var transaction = databaseThread.BeginTransaction();
#if SQLDATABASE
                        databaseThread.BulkInsertTrades(symbol, tradeCache, transaction);
#else
                        foreach (var trade in tradeCache)
                        {
                            databaseThread.Connection.Insert(trade, transaction);
                            GlobalData.AddTextToLogTab($"FetchTradesForOrderAsync: {symbol.Name} ORDER {orderId} TRADE {trade.TradeId} TOEGEVOEGD!");
                        }
#endif
                        if (tradeCount == 0)
                            GlobalData.AddTextToLogTab($"FetchTradesForOrderAsync: {symbol.Name} ORDER {orderId} NIET GEVONDEN! PANIC MODE?");
                        else
                            GlobalData.AddTextToLogTab($"FetchTradesForOrderAsync {symbol.Name} ORDER {orderId} {tradeCache.Count}");

                        transaction.Commit();
                    }
                    finally
                    {
                        Monitor.Exit(symbol.TradeList);
                    }
                }
            }
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("error get trades " + error.ToString()); // symbol.Text + " " + 
        }

        return tradeCount;
    }


}

#endif