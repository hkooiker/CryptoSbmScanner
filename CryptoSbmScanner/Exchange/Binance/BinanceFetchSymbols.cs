﻿using System.Text.Encodings.Web;
using System.Text.Json;

using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Exchange.Binance;

public class BinanceFetchSymbols
{
    public static async Task ExecuteAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab("Reading symbol information from Binance");
                BinanceWeights.WaitForFairWeight(1);

                using CryptoDatabase database = new();
                database.Open();

                WebCallResult<BinanceExchangeInfo> exchangeInfo = null;
                using (var client = new BinanceClient())
                {
                    exchangeInfo = await client.SpotApi.ExchangeData.GetExchangeInfoAsync();

                    if (!exchangeInfo.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting exchangeinfo " + exchangeInfo.Error + "\r\n");
                    }

                    //Zo af en toe komt er geen data of is de Data niet gezet.
                    //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    if (exchangeInfo == null || exchangeInfo.Data == null)
                        throw new ExchangeException("Geen exchange data ontvangen");
                }


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = new();
                    try
                    {
                        foreach (var binanceSymbol in exchangeInfo.Data.Symbols)
                        {
                            //string coin = MatchingSymbol(item.Name);
                            //if (coin != "")
                            {
                                ////
                                //// Summary:
                                ////     Status of a symbol
                                //    public enum SymbolStatus
                                //    {
                                //        //
                                //        // Summary:
                                //        //     Not trading yet
                                //        PreTrading = 0,
                                //        //
                                //        // Summary:
                                //        //     Trading
                                //        Trading = 1,
                                //        //
                                //        // Summary:
                                //        //     No longer trading
                                //        PostTrading = 2,
                                //        //
                                //        // Summary:
                                //        //     Not trading
                                //        EndOfDay = 3,
                                //        //
                                //        // Summary:
                                //        //     Halted
                                //        Halt = 4,
                                //        AuctionMatch = 5,
                                //        Break = 6
                                //    }

                                //Het is erg belangrijk om de delisted munten zo snel mogelijk te detecteren.
                                //(ik heb wat slechte ervaringen met de Altrady bot die op paniek pieken handelt)

                                //Eventueel symbol toevoegen
                                if (!exchange.SymbolListName.TryGetValue(binanceSymbol.Name, out CryptoSymbol symbol))
                                {
                                    symbol = new()
                                    {
                                        Exchange = exchange,
                                        ExchangeId = exchange.Id,
                                        Name = binanceSymbol.Name,
                                        Base = binanceSymbol.BaseAsset,
                                        Quote = binanceSymbol.QuoteAsset,
                                        Status = 1,
                                    };
                                }

                                //Tijdelijk alles overnemen (vanwege into nieuwe velden)
                                //De te gebruiken precisie in prijzen
                                //symbol.BaseAssetPrecision = binanceSymbol.BaseAssetPrecision;
                                //symbol.QuoteAssetPrecision = binanceSymbol.QuoteAssetPrecision;
                                // Tijdelijke fix voor Binance.net (kan waarschijnlijk weer weg)
                                //if (binanceSymbol.MinNotionalFilter != null)
                                //    symbol.MinNotional = binanceSymbol.MinNotionalFilter.MinNotional;
                                //else
                                //    symbol.MinNotional = 0;

                                //Minimale en maximale amount voor een order (in base amount)
                                symbol.QuantityMinimum = binanceSymbol.LotSizeFilter.MinQuantity;
                                symbol.QuantityMaximum = binanceSymbol.LotSizeFilter.MaxQuantity;
                                symbol.QuantityTickSize = binanceSymbol.LotSizeFilter.StepSize;

                                //Minimale en maximale prijs voor een order (in base price)
                                symbol.PriceMinimum = binanceSymbol.PriceFilter.MinPrice;
                                symbol.PriceMaximum = binanceSymbol.PriceFilter.MaxPrice;
                                symbol.PriceTickSize = binanceSymbol.PriceFilter.TickSize;

                                symbol.IsSpotTradingAllowed = binanceSymbol.IsSpotTradingAllowed;
                                symbol.IsMarginTradingAllowed = binanceSymbol.IsMarginTradingAllowed;

                                if (binanceSymbol.Status == SymbolStatus.Trading | binanceSymbol.Status == SymbolStatus.EndOfDay)
                                    symbol.Status = 1;
                                else
                                    symbol.Status = 0; //Zet de status door (PreTrading, PostTrading of Halt)

                                if (symbol.Id == 0)
                                {
#if !SQLDATABASE
                                    database.Connection.Insert(symbol, transaction);
#endif
                                    cache.Add(symbol);
                                }
                                else
                                    database.Connection.Update(symbol, transaction);
                            }
                        }
#if SQLDATABASE
                            database.BulkInsertSymbol(cache, transaction);
#endif
                        transaction.Commit();


                        // Bewaren voor debug werkzaamheden
                        {
                            string filename = GlobalData.GetBaseDir();
                            filename += @"\Binance\";
                            Directory.CreateDirectory(filename);
                            filename += "symbols.json";

                            string text = JsonSerializer.Serialize(exchangeInfo, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                            File.WriteAllText(filename, text);
                        }


                        // De nieuwe symbols toevoegen aan de lijst
                        // (omdat de symbols pas tijdens de BulkInsert een id krijgen)
                        foreach (CryptoSymbol symbol in cache)
                        {
                            GlobalData.AddSymbol(symbol);
                        }

                    }
                    catch (Exception error)
                    {
                        GlobalData.Logger.Error(error);
                        GlobalData.AddTextToLogTab(error.ToString());
                        transaction.Rollback();
                        throw;
                    }
                }

                exchange.LastTimeFetched = DateTime.UtcNow;
                database.Connection.Update(exchange);

            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab(error.ToString());
            }

        }
    }
}