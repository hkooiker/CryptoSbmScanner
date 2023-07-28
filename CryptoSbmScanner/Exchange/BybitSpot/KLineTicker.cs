﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.BybitSpot;

internal class KLineTicker : KLineTickerBase
{
    public static List<KLineTickerStream> TickerList { get; set; } = new();

    public override async Task Start()
    {
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange _))
        {
            int count = 0;
            List<Task> taskList = new();
            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                {
                    List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                    // TODO: Wellicht versnellen door een lijst van taken voor te bereiden (zie Task.WhenAll)

                    // We krijgen soms timeouts (eigenlijk de library) omdat we teveel 
                    // symbols aanbieden, daarom splitsen we het hier de lijst in twee stukken.
                    //int splitCount = 200;
                    //if (symbols.Count > splitCount)
                    //    splitCount = 1 + (symbols.Count / 2);

                    while (symbols.Count > 0)
                    {
                        KLineTickerStream ticker = new(quoteData);
                        TickerList.Add(ticker);

                        // Op deze exchange is er een limiet van 10 symbols, dus opknippen in (veel) stukjes
                        while (symbols.Count > 0)
                        {
                            CryptoSymbol symbol = symbols[0];
                            ticker.symbols.Add(symbol.Name);
                            symbols.Remove(symbol);
                            count++;

                            if (ticker.symbols.Count >= 10)
                                break;
                        }

                        Task task = Task.Run(async () => { await ticker.StartAsync(); });
                        taskList.Add(task);
                    }
                }
            }

            if (taskList.Any())
            {
                await Task.WhenAll(taskList);
                GlobalData.AddTextToLogTab(string.Format("Bybit started kline ticker stream for {0} symbols", count));
            }
        }
    }


    public override async Task Stop()
    {
        GlobalData.AddTextToLogTab("Bybit stopping kline ticker stream");
        List<Task> taskList = new();
        foreach (var ticker in TickerList)
        {
            Task task = Task.Run(async () => { await ticker.StopAsync(); });
            taskList.Add(task);
        }
        if (taskList.Any())
            await Task.WhenAll(taskList);
        TickerList.Clear();
    }


    public override void Reset()
    {
        foreach (var ticker in TickerList)
            ticker.TickerCount = 0;
    }


    public override int Count()
    {
        int TickerCount = 0;
        foreach (var ticker in TickerList)
            TickerCount += ticker.TickerCount;
        return TickerCount;
    }

}