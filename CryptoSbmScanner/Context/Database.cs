﻿using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;

namespace CryptoSbmScanner.Context;


// SqlConnection is sealed, dus dan maar via een compositie
// De SqliteConnection is niet sealed (die gebruiken we ook)

public class CryptoDatabase : IDisposable
{
    // De huidige database versie (zoals in de code is gedefinieerd)
    private readonly static int CurrentVersion = 2;

    public static void SetDatabaseDefaults()
    {
        Dapper.SqlMapper.Settings.CommandTimeout = 180;

#if !SQLDATABASE
        CryptoDatabase.BasePath = GlobalData.GetBaseDir();
        CryptoDatabase.CreateDatabase();
#endif
    }


#if SQLDATABASE
    public static string ConnectionString { get; set; } = "Server=localhost; database=cryptobot2; UID=cryptobot; password=cryptobot; TrustServerCertificate=True";
    public SqlConnection Connection { get; set; }

    public CryptoDatabase()
    {
        Connection = new(ConnectionString);
    }

    public SqlTransaction BeginTransaction()
    {
        return Connection.BeginTransaction();
    }

    public void BulkInsertSymbol(List<CryptoSymbol> cache, SqlTransaction transaction)
    {
        //De juiste seperators gebruiken voor MySql
        //Dat is het nadeel als je tekst gebruikt
        NumberFormatInfo myFormat = new();
        myFormat.NumberDecimalSeparator = ".";


        int offset = 0;
        while (offset < cache.Count)
        {
            //Forward slice
            int lastRecord = offset + 1000;
            if (lastRecord > cache.Count)
                lastRecord = cache.Count;

            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("insert into symbol");
            stringBuilder.AppendLine("([ExchangeId], [Name], [Base], [Quote], [Status], [Volume], " +
                //"[QuoteAssetPrecision], [BaseAssetPrecision],[MinNotional], " +
                "[QuantityMinimum], [QuantityMaximum], [QuantityTickSize]," +
                "[PriceMinimum],[PriceMaximum],[PriceTickSize]," +
                "[IsSpotTradingAllowed],[IsMarginTradingAllowed])");

            stringBuilder.AppendLine("output inserted.id");
            stringBuilder.AppendLine("values");

            for (int i = offset; i < lastRecord; i++)
            {
                CryptoSymbol symbol = cache[i];

                if (i > offset)
                    stringBuilder.AppendLine(",");

                stringBuilder.AppendFormat(string.Format("({0},'{1}','{2}','{3}',{4},{5}, {6},{7},{8}, {9},{11},{11}, {12},{13})",
                    symbol.Exchange.Id,
                    symbol.Name,
                    symbol.Base,
                    symbol.Quote,
                    symbol.Status.ToString(myFormat),
                    symbol.Volume.ToString(myFormat),

                    //symbol.QuoteAssetPrecision.ToString(myFormat),
                    //symbol.BaseAssetPrecision.ToString(myFormat),
                    //symbol.MinNotional.ToString(myFormat),

                    symbol.QuantityMinimum.ToString(myFormat),
                    symbol.QuantityMaximum.ToString(myFormat),
                    symbol.QuantityTickSize.ToString(myFormat),

                    symbol.PriceMinimum.ToString(myFormat),
                    symbol.PriceMaximum.ToString(myFormat),
                    symbol.PriceTickSize.ToString(myFormat),

                    Convert.ToInt32(symbol.IsSpotTradingAllowed),
                    Convert.ToInt32(symbol.IsMarginTradingAllowed)
                ));
            }

            using (SqlCommand command = new(stringBuilder.ToString(), Connection))
            {
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;

                //Als laatste de ID's van de objecten bijwerken
                /*
                //MYSQL:
                command.ExecuteNonQuery();
                int LastId = (int)command.LastInsertedId;
                foreach (Symbol symbol in symbolCache)
                    symbol.Id = LastId++;
                */

                //MSSQL: 
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        for (int i = offset; i < lastRecord; i++)
                        {
                            CryptoSymbol symbol = cache[i];
                            if (reader.Read())
                            {
                                symbol.Id = reader.GetInt32(0);
                            }
                        }
                    }
                    reader.Close();
                }

            }

            offset += 1000;
        }
    }



    public void BulkInsertCandles(List<CryptoCandle> cache, SqlTransaction transaction)
    {
        //De juiste seperators gebruiken voor MySql
        //Dat is het nadeel als je tekst gebruikt
        NumberFormatInfo mySqlFormat = new();
        mySqlFormat.NumberDecimalSeparator = ".";


        int offset = 0;
        while (offset < cache.Count)
        {
            //Forward slice
            int lastRecord = offset + 1000;
            if (lastRecord > cache.Count)
                lastRecord = cache.Count;

            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("INSERT INTO Candle ([ExchangeId], [SymbolId], [OpenTime], [IntervalId], [Open], [High], [Low], [Close], [Volume])");
            stringBuilder.AppendLine("output inserted.id");
            stringBuilder.AppendLine("VALUES");

            for (int i = offset; i < lastRecord; i++)
            {
                CryptoCandle candle = cache[i];

                if (i > offset)
                    stringBuilder.AppendLine(",");

                stringBuilder.AppendFormat(string.Format("({0},{1},{2},{3},{4},{5},{6},{7},{8})",
                    candle.ExchangeId,
                    candle.SymbolId,
                    //candle.Date.ToString("yyyy-MM-dd HH:mm"), //:ss nauwkeurigheid is tot dusver beperkt tot minuten, direct afronding kwijt
                    candle.OpenTime,
                    candle.IntervalId,
                    candle.Open.ToString(mySqlFormat),
                    candle.High.ToString(mySqlFormat),
                    candle.Low.ToString(mySqlFormat),
                    candle.Close.ToString(mySqlFormat),
                    candle.Volume.ToString(mySqlFormat)
                ));
                //MySqlHelper.EscapeString("test")
            }


            using (SqlCommand command = new(stringBuilder.ToString(), Connection))
            {
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                //command.ExecuteNonQuery();
                //int LastId = (int)command.LastInsertedId;

                ////Als laatste de ID's van de objecten bijwerken
                //foreach (Candle candle in candleCache)
                //    candle.Id = LastId++;


                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        for (int i = offset; i < lastRecord; i++)
                        {
                            CryptoCandle candle = cache[i];
                            if (reader.Read())
                            {
                                candle.Id = reader.GetInt32(0);
                            }
                        }
                    }
                    reader.Close();
                }
            }

            offset += 1000;
        }
    }




    public void BulkInsertTrades(CryptoSymbol symbol, List<CryptoTrade> cache, SqlTransaction transaction)
    {
        //De juiste seperators gebruiken voor MySql
        //Dat is het nadeel als je tekst gebruikt
        NumberFormatInfo mySqlFormat = new();
        mySqlFormat.NumberDecimalSeparator = ".";


        int offset = 0;
        while (offset < cache.Count)
        {
            //Forward slice
            int lastRecord = offset + 1000;
            if (lastRecord > cache.Count)
                lastRecord = cache.Count;

            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("INSERT INTO Trade (" +
                "ExchangeId, SymbolId, TradeId,OrderId," + //OrderListId,
                "Price, Quantity, QuoteQuantity, " +
                "Commission, CommissionAsset, " +
                "TradeTime, " +
                "IsBuyer, IsMaker)");
            stringBuilder.AppendLine("output inserted.id");
            stringBuilder.AppendLine("VALUES");

            for (int i = offset; i < lastRecord; i++)
            {
                CryptoTrade trade = cache[i];

                if (i > offset)
                    stringBuilder.AppendLine(",");

                stringBuilder.AppendFormat(string.Format("({0},{1},{2},{3},{4},{5},{6},{7},'{8}','{9}',{10},{11})",
                    trade.Exchange.Id,
                    trade.Symbol.Id,
                    trade.TradeId.ToString(mySqlFormat),
                    trade.OrderId.ToString(mySqlFormat),
                    //trade.OrderListId.ToString(mySqlFormat),

                    trade.Price.ToString(mySqlFormat),
                    trade.Quantity.ToString(mySqlFormat),
                    trade.QuoteQuantity.ToString(mySqlFormat),
                    trade.Commission.ToString(mySqlFormat),
                    trade.CommissionAsset,

                    trade.TradeTime.ToString("yyyyMMdd HH:mm:ss"),

                    Convert.ToInt32(trade.Side)
                ));
            }


            using (SqlCommand command = new(stringBuilder.ToString(), Connection))
            {
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                //command.ExecuteNonQuery();
                //int LastId = (int)command.LastInsertedId;

                ////Als laatste de ID's van de objecten bijwerken
                //foreach (Trade trade in Cache)
                //    trade.Id = LastId++;
                //MSSQL: 
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        for (int i = offset; i < lastRecord; i++)
                        {
                            CryptoTrade trade = cache[i];
                            if (reader.Read())
                            {
                                trade.Id = reader.GetInt32(0);
                            }
                        }
                    }
                    reader.Close();
                }
            }

            offset += 1000;
        }
    }

    //public void BulkInsertOrders(Symbol symbol, List<Order> cache, SqlTransaction transaction)
    //{
    //    //De juiste seperators gebruiken voor MySql
    //    //Dat is het nadeel als je tekst gebruikt
    //    NumberFormatInfo mySqlFormat = new();
    //    mySqlFormat.NumberDecimalSeparator = ".";


    //    int offset = 0;
    //    while (offset < cache.Count)
    //    {
    //        //Forward slice
    //        int lastRecord = offset + 1000;
    //        if (lastRecord > cache.Count)
    //            lastRecord = cache.Count;

    //        StringBuilder stringBuilder = new();
    //        stringBuilder.AppendLine("INSERT INTO Order (ExchangeId, SymbolId, CreateTime, " +
    //            "IcebergQuantity, StopPrice, Side, Type,TimeInForce,Status," +
    //            "QuoteQuantity,QuoteQuantityFilled,QuantityFilled,Quantity,Price," +
    //            "OriginalClientOrderId,OrderListId,ClientOrderId,OrderId,UpdateTime,IsWorking" +
    //            ")");
    //        stringBuilder.AppendLine("output inserted.id");
    //        stringBuilder.AppendLine("VALUES");

    //        for (int i = offset; i < lastRecord; i++)
    //        {
    //            Order order = cache[i];

    //            if (i > offset)
    //                stringBuilder.AppendLine(",");


    //            stringBuilder.AppendFormat(string.Format("({0},{1},'{2}', " +
    //                " {3},{4},{5},{6},{7},{8}," +
    //                "{9},{10},{11},{12},{13}," +
    //                "'{14}',{15},'{16}',{17},'{18}',{19}" +
    //                ")",
    //                order.Exchange.Id,
    //                order.Symbol.Id,
    //                order.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),

    //                order.IcebergQuantity.ToString(mySqlFormat),
    //                order.StopPrice?.ToString(mySqlFormat),
    //                Convert.ToInt32(order.Side),
    //                Convert.ToInt32(order.Type),
    //                Convert.ToInt32(order.TimeInForce),
    //                Convert.ToInt32(order.Status),

    //                order.QuoteQuantity.ToString(mySqlFormat),
    //                order.QuoteQuantityFilled.ToString(mySqlFormat),
    //                order.QuantityFilled.ToString(mySqlFormat),
    //                order.Quantity.ToString(mySqlFormat),
    //                order.Price.ToString(mySqlFormat),

    //                order.OriginalClientOrderId,
    //                order.OrderListId,
    //                order.ClientOrderId,
    //                order.OrderId,
    //                order.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss"),
    //                Convert.ToInt32(order.IsWorking)
    //            ));

    //        }


    //        using (SqlCommand command = new(stringBuilder.ToString(), MyConnection))
    //        {
    //            command.Transaction = transaction;
    //            command.CommandType = CommandType.Text;
    //            //command.ExecuteNonQuery();
    //            //int LastId = (int)command.LastInsertedId;

    //            ////Als laatste de ID's van de objecten bijwerken
    //            //foreach (Order order in Cache)
    //            //    order.Id = LastId++;
    //            //MSSQL: 
    //            using (SqlDataReader reader = command.ExecuteReader())
    //            {
    //                if (reader.HasRows)
    //                {
    //                    for (int i = offset; i < lastRecord; i++)
    //                    {
    //                        Order order = cache[i];
    //                        if (reader.Read())
    //                        {
    //                            order.Id = reader.GetInt32(0);
    //                        }
    //                    }
    //                }
    //                reader.Close();
    //            }
    //        }

    //        offset += 1000;
    //    }
    //}

#else
    public static string BasePath { get; set; }
    public SqliteConnection Connection { get; set; }

    public CryptoDatabase()
    {
        Connection = new("Filename=" + BasePath + "CryptoSbmScanner.db;Mode=ReadWriteCreate");
    }

    public SqliteTransaction BeginTransaction()
    {
        return Connection.BeginTransaction();
    }

 //   public void BulkInsertSymbol(List<CryptoSymbol> cache, SqliteTransaction transaction)
	//{
 //       // todo
	//}

 //   public void BulkInsertTrades(CryptoSymbol symbol, List<CryptoTrade> cache, SqliteTransaction transaction)
 //   {
 //       // todo
 //   }

 //   public void BulkInsertCandles(List<CryptoCandle> cache, SqliteTransaction transaction)
 //   {
 //       // todo
 //   }

#endif


    public void Dispose()
    {
        //Dispose(true);
        Connection.Dispose();
    }

    public void Open()
    {
        Connection.Open();
    }

    public void Close()
    {
        Connection.Close();
    }

    private static void CreateTableVersion(CryptoDatabase connection)
    {
        string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Version';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            connection.Connection.Execute("CREATE TABLE [Version] (" +
                "Id integer primary key autoincrement not null, " +
                "Version INTEGER NOT NULL" +
            ")");

            // De exchanges moeten aanwezig zijn na initialisatie
            using var transaction = connection.Connection.BeginTransaction();
            CryptoVersion databaseVersion = new()
            {
                Version = 1,
            };
            connection.Connection.Insert(databaseVersion, transaction);
            transaction.Commit();
        }
    }

    private static void CreateTableInterval(CryptoDatabase connection)
    {
        string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Interval';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            connection.Connection.Execute("CREATE TABLE [Interval] (" +
                "Id integer primary key autoincrement not null, " +
                "IntervalPeriod INTEGER NOT NULL," +
                "Name TEXT NOT NULL," +
                "Duration INTEGER NOT NULL," +
                "ConstructFromId INTEGER NULL," +
                "FOREIGN KEY(ConstructFromId) REFERENCES Interval(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxIntervalId ON Interval(Id)");
            connection.Connection.Execute("CREATE INDEX IdxIntervalName ON Interval(Name)");
            connection.Connection.Execute("CREATE INDEX IdxIntervalConstructFromId ON Interval(ConstructFromId)");


            using var transaction = connection.BeginTransaction();

            // De intervallen moeten aanwezig zijn na initialisatie
            List<CryptoInterval> IntervalList = new()
            {
                CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1m, "1m", 1 * 60, null) //0
            };
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval2m, "2m", 2 * 60, IntervalList[0])); //1
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval3m, "3m", 3 * 60, IntervalList[0])); //2
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval5m, "5m", 5 * 60, IntervalList[0])); //3
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval10m, "10m", 10 * 60, IntervalList[3])); //4
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval15m, "15m", 15 * 60, IntervalList[3]));  //5
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval30m, "30m", 30 * 60, IntervalList[5])); //6
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1h, "1h", 01 * 60 * 60, IntervalList[6])); //7
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval2h, "2h", 02 * 60 * 60, IntervalList[7])); //8
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval3h, "3h", 03 * 60 * 60, IntervalList[7])); //9
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval4h, "4h", 04 * 60 * 60, IntervalList[8])); //10
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval6h, "6h", 06 * 60 * 60, IntervalList[9])); //11
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval8h, "8h", 08 * 60 * 60, IntervalList[10])); //12
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval12h, "12h", 12 * 60 * 60, IntervalList[10])); //13
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1d, "1d", 1 * 24 * 60 * 60, IntervalList[11])); //14

            // iets teveel, niet relevant voor deze tool
            //IntervalList.Add(new CryptoInterval(CryptoIntervalPeriod.interval3d, "3d", 3 * 24 * 60 * 60, IntervalList[12])); //13
            //IntervalList.Add(new Interval(IntervalPeriod.interval1w, "1w", 7 * 24 * 60 * 60, IntervalList[12], (decimal)4)); //14
            //IntervalList.Add(new Interval(IntervalPeriod.interval2w, "2w", 14 * 24 * 60 * 60, IntervalList[14], (decimal)4)); //15

            foreach (CryptoInterval interval in IntervalList)
            {
                connection.Connection.Insert(interval, transaction);

                // bijwerken
                foreach (CryptoInterval interval2 in IntervalList)
                {
                    if (interval2.ConstructFrom != null)
                        interval2.ConstructFromId = interval2.ConstructFrom.Id;
                }
            }
            transaction.Commit();

        }
    }

    private static void CreateTableExchange(CryptoDatabase connection)
    {
        // ****************************************************
        // Exchange (voorlopig alleen Binance, daar is nu alels op afgestemd)
        string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Exchange';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            connection.Connection.Execute("CREATE TABLE [Exchange] (" +
                 "Id integer primary key autoincrement not null, " +
                "LastTimeFetched TEXT NULL," +                 
                 "Name TEXT not NULL" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxExchangeId ON Exchange(Id)");
            connection.Connection.Execute("CREATE INDEX IdxExchangeName ON Exchange(Name)");


            // De ondersteunde exchanges toevoegen
            // NB: In de code wordt aannames van de ID gedaan dus gaarne niet knutselen met volgorde
            using var transaction = connection.Connection.BeginTransaction();
            Model.CryptoExchange exchange = new() { Name = "Binance" };
            connection.Connection.Insert(exchange, transaction);

            exchange = new() { Name = "Bybit Spot" };
            connection.Connection.Insert(exchange, transaction);

            exchange = new() { Name = "Bybit Futures" };
            connection.Connection.Insert(exchange, transaction);

            // Kucoin wordt verderop in het proces verwijderd (totdat we de exchange onder de knie hebben)
            exchange = new() { Name = "Kucoin" };
            connection.Connection.Insert(exchange, transaction);
            transaction.Commit();
        }
    }

    private static void CreateTableTradeAccount(CryptoDatabase connection)
    {
        // ****************************************************
        // Exchange (voorlopig alleen Binance, daar is nu alels op afgestemd)
        string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'TradeAccount';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            connection.Connection.Execute("CREATE TABLE [TradeAccount] (" +
                "Id integer primary key autoincrement not null, " +
                "Name TEXT not NULL," +
                "Short TEXT not NULL," +
                "ExchangeId INTEGER NOT NULL," +
                "AccountType INTEGER not NULL," +
                "TradeAccountType Integer not NULL" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxTradeAccountId ON TradeAccount(Id)");
            connection.Connection.Execute("CREATE INDEX IdxTradeAccountName ON TradeAccount(Name)");
            connection.Connection.Execute("CREATE INDEX IdxTradeAccountExchangeId ON TradeAccount(ExchangeId)");


            // De exchanges moeten aanwezig zijn na initialisatie
            using var transaction = connection.Connection.BeginTransaction();
            CryptoTradeAccount tradeAccount = new()
            {
                Name = "Binance trading",
                Short = "Trading",
                ExchangeId = 1,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.RealTrading,
            };
            connection.Connection.Insert(tradeAccount, transaction);

            tradeAccount = new()
            {
                Name = "Binance paper",
                Short = "Pater",
                ExchangeId = 1,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.PaperTrade,
            };
            connection.Connection.Insert(tradeAccount, transaction);

            tradeAccount = new()
            {
                Name = "Binance backtest",
                Short = "Backtest",
                ExchangeId = 1,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.BackTest,
            };
            connection.Connection.Insert(tradeAccount, transaction);



            tradeAccount = new()
            {
                Name = "Bybit Spot trading",
                Short = "Trading",
                ExchangeId = 1,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.RealTrading,
            };
            connection.Connection.Insert(tradeAccount, transaction);

            tradeAccount = new()
            {
                Name = "Bybit Spot paper",
                Short = "Pater",
                ExchangeId = 2,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.PaperTrade,
            };
            connection.Connection.Insert(tradeAccount, transaction);

            tradeAccount = new()
            {
                Name = "Bybit Spot backtest",
                Short = "Backtest",
                ExchangeId = 2,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.BackTest,
            };
            connection.Connection.Insert(tradeAccount, transaction);


            tradeAccount = new()
            {
                Name = "Bybit Futures trading",
                Short = "Trading",
                ExchangeId = 3,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.RealTrading,
            };
            connection.Connection.Insert(tradeAccount, transaction);

            tradeAccount = new()
            {
                Name = "Bybit Futures paper",
                Short = "Pater",
                ExchangeId = 3,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.PaperTrade,
            };
            connection.Connection.Insert(tradeAccount, transaction);

            tradeAccount = new()
            {
                Name = "Bybit Futures backtest",
                Short = "Backtest",
                ExchangeId = 3,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.BackTest,
            };
            connection.Connection.Insert(tradeAccount, transaction);



            tradeAccount = new()
            {
                Name = "Kucoin trading",
                Short = "Trading",
                ExchangeId = 4,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.RealTrading,
            };
            connection.Connection.Insert(tradeAccount, transaction);

            tradeAccount = new()
            {
                Name = "Kucoin paper",
                Short = "Pater",
                ExchangeId = 4,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.PaperTrade,
            };
            connection.Connection.Insert(tradeAccount, transaction);

            tradeAccount = new()
            {
                Name = "Kucoin backtest",
                Short = "Backtest",
                ExchangeId = 4,
                AccountType = CryptoAccountType.Spot,
                TradeAccountType = CryptoTradeAccountType.BackTest,
            };
            connection.Connection.Insert(tradeAccount, transaction);
            transaction.Commit();
        }
    }

    private static void CreateTableSymbol(CryptoDatabase connection)
    {
        // ****************************************************
        // Symbol (wat muntjes van Binance met aardig wat attributen)
        string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Symbol';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            connection.Connection.Execute("CREATE TABLE [Symbol] (" +
                "Id INTEGER primary key autoincrement not null, " +
                "ExchangeId INTEGER NOT NULL, " +
                "Name TEXT NOT NULL," +
                "Base TEXT NOT NULL," +
                "Quote TEXT NOT NULL," +
                "Status INTEGER NOT NULL," +
                "Volume TEXT NULL," +

                // vervallen, niet altijd gevuld en daarom onbetrouwbaar
                //"BaseAssetPrecision integer NULL," +
                //"QuoteAssetPrecision integer NULL," +
                //"MinNotional TEXT NULL," +

                "PriceMinimum TEXT NULL," +
                "PriceMaximum TEXT NULL," +
                "PriceTickSize TEXT NULL," +

                "QuantityMinimum TEXT NULL," +
                "QuantityMaximum TEXT NULL," +
                "QuantityTickSize TEXT NULL," +

                "LastTradefetched TEXT NULL," +
                "IsSpotTradingAllowed INTEGER NULL," +
                "IsMarginTradingAllowed INTEGER NULL," +
                "LastPrice TEXT NULL," +
                "TrendInfoDate TEXT NULL," +
                "TrendPercentage TEXT NULL," +
                "LastTradeDate TEXT NULL," +

                // Bybit Futures, ondersteunen van de FundingRate en FundingInterval
                "FundingRate TEXT NULL," +
                "FundingInterval TEXT NULL, " +

                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxSymbolId ON Symbol(Id)");
            connection.Connection.Execute("CREATE INDEX IdxSymbolExchangeId ON Symbol(ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxSymbolName ON Symbol(Name)");
            connection.Connection.Execute("CREATE INDEX IdxSymbolBase ON Symbol(Base)");
            connection.Connection.Execute("CREATE INDEX IdxSymbolQuote ON Symbol(Quote)");
        }
    }

    //private static void CreateTableSymbolInterval(CryptoDatabase connection)
    //{
    //    // ****************************************************
    //    // SymbolInterval (administratie, maar overlapt met de bestanden, via bestand is beter denk ik, rest is overkill)
    //    string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'SymbolInterval';").FirstOrDefault();
    //    if (string.IsNullOrEmpty(tableName))
    //    {
    //        connection.Connection.Execute("CREATE TABLE [SymbolInterval] (" +
    //            "Id INTEGER primary key autoincrement not null, " +
    //            "ExchangeId INTEGER NOT NULL, " +
    //            "SymbolId INTEGER NOT NULL, " +
    //            "IntervalId INTEGER NOT NULL, " +
    //            "TrendInfoDate TEXT NULL," +
    //            "TrendIndicator Integer NULL," +
    //            "LastCandleSynchronized TEXT NULL," + // overlapt
    //            "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)" +
    //            "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
    //            "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)" +
    //        ")");
    //        connection.Connection.Execute("CREATE INDEX IdxSymbolIntervalId ON SymbolInterval(Id)");
    //        connection.Connection.Execute("CREATE INDEX IdxSymbolIntervalExchangeId ON SymbolInterval(ExchangeId)");
    //        connection.Connection.Execute("CREATE INDEX IdxSymbolIntervalSymbolId ON SymbolInterval(SymbolId)");
    //        connection.Connection.Execute("CREATE INDEX IdxSymbolIntervalIntervalId ON SymbolInterval(IntervalId)");
    //    }
    //}


    private static void CreateTableSignal(CryptoDatabase connection)
    {
        string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Signal';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            connection.Connection.Execute("CREATE TABLE [Signal] (" +
                "Id integer primary key autoincrement not null, " +
                "ExchangeId INTEGER NOT NULL," +
                "SymbolId INTEGER NOT NULL," +
                "IntervalId INTEGER NULL," +

                "IsInvalid INTEGER NULL," +

                "EventTime bigint NOT NULL," +
                "Side INTEGER NOT NULL," +
                "Price TEXT NOT NULL," +
                "EventText TEXT NULL," +

                "Last24HoursChange TEXT NULL," +
                "Last48Hours TEXT NULL," +
                "Last24HoursEffective TEXT NULL," +
                "Volume TEXT NULL," +
                "OpenDate TEXT NULL," +
                "CloseDate TEXT NULL," +
                "ExpirationDate TEXT NULL," +

                "TrendIndicator INTEGER NULL," +
                "TrendPercentage TEXT NULL," +

                "infodate TEXT NULL," +
                "BarcodePercentage TEXT NULL," +
                "Strategy INTEGER NULL," +
                "CandlesWithZeroVolume INTEGER NULL," +
                "CandlesWithFlatPrice INTEGER NULL," +
                "AboveBollingerBandsSma INTEGER NULL," +
                "AboveBollingerBandsUpper INTEGER NULL," +

                "StochSignal TEXT NULL," +
                "StochOscillator TEXT NULL," +
                //"BollingerBandsLowerBand TEXT NULL," +
                //"BollingerBandsUpperBand TEXT NULL," +
                "BollingerBandsDeviation TEXT NULL," +
                "BollingerBandsPercentage TEXT NULL," +

                "KeltnerLowerBand TEXT NULL," +
                "KeltnerUpperBand TEXT NULL," +

                "Rsi TEXT NULL," +
                "SlopeRsi TEXT NULL," +
                "Psar TEXT NULL," +
                "Psar2 TEXT NULL," +

                "sma20 TEXT NULL," +
                "ema50 TEXT NULL," +
                "ema100 TEXT NULL," +
                "ema200 TEXT NULL," +
                "SlopeEma20 TEXT NULL," +
                "SlopeEma50 TEXT NULL," +

                "ema20 TEXT NULL," +
                "sma200 TEXT NULL," +
                "sma100 TEXT NULL," +
                "sma50 TEXT NULL," +
                "SlopeSma50 TEXT NULL," +
                "SlopeSma20 TEXT NULL," +

                "FluxIndicator5m TEXT NULL," +
                

                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)" +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
                "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxSignalId ON Signal(Id)");
            connection.Connection.Execute("CREATE INDEX IdxSignalExchangeId ON Signal(ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxSignalSymbolId ON Signal(SymbolId)");
            connection.Connection.Execute("CREATE INDEX IdxSignalIntervalId ON Signal(IntervalId)");
        }
    }


    private static void CreateTablePosition(CryptoDatabase connection)
    {
        string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Position';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            connection.Connection.Execute("CREATE TABLE [Position] (" +
                "Id integer primary key autoincrement not null, " +
                "TradeAccountId integer, " +
                
                "CreateTime TEXT NOT NULL, " +
                "CloseTime TEXT NULL, " +

                "ExchangeId Integer NOT NULL," +
                "SymbolId Integer NOT NULL, " +
                "IntervalId Integer NULL," +
                "Status INTEGER NOT NULL, " +
                "Side INTEGER NOT NULL, " +
                "Strategy INTEGER NOT NULL, " +
                "data TEXT NULL," +

                "BuyPrice TEXT NULL, " +
                "Quantity TEXT NULL, " +
                "BuyAmount TEXT NULL, " +
                "SellPrice TEXT NULL, " +
                "PartCount INTEGER NULL, " +
                "Profit TEXT NULL, " +
                "BreakEvenPrice TEXT NULL, " +

                "Invested TEXT NULL, " +
                "Commission TEXTNULL, " +
                "Returned TEXT NULL, " +
                "Percentage TEXT NULL, " +
                "FOREIGN KEY(TradeAccountId) REFERENCES TradeAccount(Id)," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)," +
                "FOREIGN KEY(IntervalId) REFERENCES Interval(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxPositionId ON Position(Id)");
            connection.Connection.Execute("CREATE INDEX IdxPositionExchangeId ON Position(ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxPositionSymbolId ON Position(SymbolId)");
            connection.Connection.Execute("CREATE INDEX IdxPositionCreateTime ON Position(CreateTime)");
            connection.Connection.Execute("CREATE INDEX IdxPositionCloseTime ON Position(CloseTime)");
            connection.Connection.Execute("CREATE INDEX IdxPositionTradeAccountId ON Position(TradeAccountId)");
        }
    }

    private static void CreateTablePositionPart(CryptoDatabase connection)
    {
        string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'PositionPart';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            connection.Connection.Execute("CREATE TABLE [PositionPart] (" +
                "Id integer primary key autoincrement not null, " +
                "PositionId Integer NOT NULL," +
                "ExchangeId Integer NOT NULL," +
                "SymbolId Integer NOT NULL, " +

                "Side INTEGER NOT NULL, " +
                "Name TEXT NULL," +
                "CreateTime TEXT NOT NULL, " +
                "CloseTime TEXT NULL, " +
                "Status INTEGER NOT NULL, " +

                "Invested TEXT NULL, " +
                "Commission TEXTNULL, " +
                "Returned TEXT NULL, " +
                "Profit TEXT NULL, " +
                "Percentage TEXT NULL, " +

                "Quantity TEXT NULL, " +
                "BreakEvenPrice TEXT NULL, " +

                "BuyPrice TEXT NOT NULL, " +
                "BuyAmount TEXT NULL, " +
                "SellPrice TEXT NULL, " +

                "FOREIGN KEY(PositionId) REFERENCES Position(Id)," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxPositionPartId ON PositionPart(Id)");
            connection.Connection.Execute("CREATE INDEX IdxPositionPartExchangeId ON PositionPart(ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxPositionPartSymbolId ON PositionPart(SymbolId)");
            connection.Connection.Execute("CREATE INDEX IdxPositionPartCreateTime ON PositionPart(CreateTime)");
            connection.Connection.Execute("CREATE INDEX IdxPositionPartCloseTime ON PositionPart(CloseTime)");
        }
    }

    private static void CreateTablePositionStep(CryptoDatabase connection)
    {
        string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'PositionStep';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            connection.Connection.Execute("CREATE TABLE [PositionStep] (" +
                "Id integer primary key autoincrement not null, " +
                "PositionId integer NOT NULL," +
                "PositionPartId integer NOT NULL," +
                "CreateTime TEXT NOT NULL," +
                "CloseTime TEXT NULL," +
                "Name TEXT NOT NULL," +
                "Status INTEGER NOT NULL," +
                "Side INTEGER NOT NULL," +
                "OrderType INTEGER NOT NULL," +
                "Price TEXT NOT NULL," +
                "StopPrice TEXT NULL," +
                "StopLimitPrice TEXT NULL," +
                "Quantity TEXT NOT NULL," +
                "Commission NOT NULL," +
                "QuantityFilled TEXT NOT NULL," +
                "QuoteQuantityFilled TEXT NOT NULL," +
                // Vanwege Papertrading nullable
                "OrderId TEXT NULL," +
                "Order2Id TEXT NULL," +
                //"OrderListId TEXT NULL," +
                "Trailing INTEGER NULL," +
                "FOREIGN KEY(PositionId) REFERENCES Position(Id)," +
                "FOREIGN KEY(PositionPartId) REFERENCES PositionPart(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxPositionStepId ON Position(Id)");
            connection.Connection.Execute("CREATE INDEX IdxPositionStepPositionId ON PositionStep(PositionId)");
            connection.Connection.Execute("CREATE INDEX IdxPositionStepCreateTime ON PositionStep(CreateTime)");
            connection.Connection.Execute("CREATE INDEX IdxPositionStepCloseTime ON PositionStep(CloseTime)");
            connection.Connection.Execute("CREATE INDEX IdxPositionStepPositionPartId ON PositionStep(PositionPartId)");
        }
    }

    //private static void CreateTableOrder(CryptoDatabase connection)
    //{
    // ****************************************************
    // Order (de order zoals Binance geplaatst heeft <kan geannuleerd zijn>)
    // Besloten dat momenteel alleen trades van belang zijn

    //string tableName = Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Order';").FirstOrDefault();
    //if (string.IsNullOrEmpty(tableName))
    //{
    //    Connection.Execute("CREATE TABLE [Order] (" +
    //        "Id integer primary key autoincrement not null, " +
    //        "ExchangeId integer NOT NULL," +
    //        "SymbolId integer NOT NULL," +
    //        "CreateTime TEXT NOT NULL," +
    //        "IcebergQuantity TEXT NOT NULL," +
    //        "StopPrice TEXT NOT NULL," +
    //        "Side integer NULL," +
    //        "Type integer NULL," +
    //        "TimeInForce integer NULL," +
    //        "Status integer NULL," +
    //        "QuoteQuantity TEXT NOT NULL," +
    //        "QuoteQuantityFilled TEXT NOT NULL," +
    //        "QuantityFilled TEXT NOT NULL," +
    //        "Quantity TEXT NOT NULL," +
    //        "Price TEXT NOT NULL," +
    //        "OriginalClientOrderId TEXT NULL," +
    //        "OrderListId TEXT NOT NULL," +
    //        "ClientOrderId TEXT NULL," +
    //        "OrderId TEXT NOT NULL," +
    //        "UpdateTime TEXT NOT NULL," +
    //        "IsWorking INTEGER NOT NULL," +
    //        "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
    //        "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
    //    ")");
    //    Connection.Execute("CREATE INDEX IdxOrderId ON [Order](Id)");
    //    Connection.Execute("CREATE INDEX IdxOrderExchangeId ON [Order](ExchangeId)");
    //    Connection.Execute("CREATE INDEX IdxOrderSymbolId ON [Order](SymbolId)");
    //    Connection.Execute("CREATE INDEX IdxOrderCreateTime ON [Order](CreateTime)");
    //    Connection.Execute("CREATE INDEX IdxOrderUpdateTime ON [Order](UpdateTime)");
    //}
    //}

    private static void CreateTableTrade(CryptoDatabase connection)
    {
        // ****************************************************
        // Trade (de trade zoals Binance die uitgevoerd heeft)

        string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Trade';").FirstOrDefault();
        if (string.IsNullOrEmpty(tableName))
        {
            connection.Connection.Execute("CREATE TABLE [Trade] (" +
                "Id integer primary key autoincrement not null, " +
                "TradeTime TEXT NOT NULL," +
                
                "TradeAccountId Integer NOT NULL," +
                "ExchangeId Integer NOT NULL," +
                "SymbolId Integer NOT NULL," +

                "Price TEXT NOT NULL," +
                "Quantity TEXT NOT NULL," +
                "QuoteQuantity TEXT NOT NULL," +
                // TODO: Universele iets (omrekenen!)
                "Commission TEXT NOT NULL," +
                "CommissionAsset TEXT NOT NULL," +

                // Erg Binance specifiek?
                "Side Integer NOT NULL," +
                //"IsMaker Integer NOT NULL," + // ivm berekening fee (maar is al in de prijs verwerkt, wellicht om achteraf iets te beredeneren qua fee?)

                "TradeId TEXT NOT NULL," +
                "OrderId TEXT NOT NULL," +
                //"OrderListId TEXT NULL," +
                
                "FOREIGN KEY(TradeAccountId) REFERENCES TradeAccount(Id)," +
                "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
                "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
            ")");
            connection.Connection.Execute("CREATE INDEX IdxTradeId ON [Trade](Id)");
            connection.Connection.Execute("CREATE INDEX IdxTradeExchangeId ON [Trade](ExchangeId)");
            connection.Connection.Execute("CREATE INDEX IdxTradeSymbolId ON [Trade](SymbolId)");
            connection.Connection.Execute("CREATE INDEX IdxTradeTradeTime ON [Trade](TradeTime)");
            connection.Connection.Execute("CREATE INDEX IdxTradeTradeAccountId ON [Trade](TradeAccountId)");
         }

    }

    //private static void CreateTableBalancing(CryptoDatabase connection)
    //{
    //    //// ****************************************************
    //    //// Balance (echt? weet niet waarom we dit op deze manier opslaan, balanceren doe je binnen groep, die mis ik, een oude versie wellicht?)
    //    //string tableName = connection.Connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Balance';").FirstOrDefault();
    //    //if (string.IsNullOrEmpty(tableName))
    //    //{
    //    //    connection.Connection.Execute("CREATE TABLE [Balance] (" +
    //    //        "Id integer primary key autoincrement not null, " +
    //    //        "ExchangeId Integer NOT NULL," +
    //    //        "SymbolId Integer NOT NULL," +
    //    //        "EventTime TEXT NOT NULL," +
    //    //        "Name TEXT NOT NULL," +
    //    //        "Price TEXT NOT NULL," +
    //    //        "Quantity TEXT NOT NULL," +
    //    //        "QuoteQuantity TEXT NOT NULL," +
    //    //        "InvestedQuantity TEXT NULL," +
    //    //        "InvestedValue TEXT NULL," +
    //    //        "UsdtValue TEXT NULL," +
    //    //        "FOREIGN KEY(ExchangeId) REFERENCES Exchange(Id)," +
    //    //        "FOREIGN KEY(SymbolId) REFERENCES Symbol(Id)" +
    //    //    ")");
    //    //    connection.Connection.Execute("CREATE INDEX IdxBalanceId ON [Balance](Id)");
    //    //    connection.Connection.Execute("CREATE INDEX IdxBalanceExchangeId ON [Balance](ExchangeId)");
    //    //    connection.Connection.Execute("CREATE INDEX IdxBalanceSymbolId ON [Balance](SymbolId)");
    //    //    connection.Connection.Execute("CREATE INDEX IdxBalanceEventTime ON [Balance](EventTime)");
    //    //}
    //}

    public static void CreateDatabase()
    {
        // Sqlite gaat afwijkend met datatypes om, zie https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types

        using var connection = new CryptoDatabase();
        connection.Open();

        CreateTableInterval(connection); // (met een hardcoded lijst, voorlopig prima)
        CreateTableExchange(connection); // (met een hardcoded lijst, voorlopig prima)

        CreateTableSymbol(connection);
        //CreateTableSymbolInterval(connection); -- opgeslagen in files, prima zo
        CreateTableSignal(connection);

        CreateTableTradeAccount(connection);
        CreateTablePosition(connection);
        CreateTablePositionPart(connection);
        CreateTablePositionStep(connection);
        
        //CreateTableOrder(connection); -- vervallen
        CreateTableTrade(connection);

        //CreateTableAsset(connection); -- todo ooit
        //CreateTableBalancing(connection); -- todo ooit
        CreateTableVersion(connection);


        // Indien noodzakelijk database upgraden 
        Migration.Execute(connection, CurrentVersion);
    }

}