﻿using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using CryptoSbmScanner.TradingView;

using Dapper.Contrib.Extensions;

using System.Text.Encodings.Web;
using System.Text.Json;

namespace CryptoSbmScanner.Intern;


/// <summary>
/// Om vanuit de threads tekst in het main scherm te zetten
/// TODO Betere logger te gebruiken, maar dit werkt (voorlopig)
/// </summary>
public delegate void AddTextEvent(string text, bool extraLineFeed = false);

public delegate void PlayMediaEvent(string text, bool test = false);

/// <summary>
/// Om vanuit de threads de timer voor ophalen Candles te disablen
/// </summary>
public delegate void SetCandleTimerEnable(bool value);



/// De laatst berekende barometer standen
public class BarometerData
{
    public long? PriceDateTime { get; set; }
    public decimal? PriceBarometer { get; set; }

    public long? VolumeDateTime { get; set; }
    public decimal? VolumeBarometer { get; set; }
}


static public class GlobalData
{
    // Emulator kan alleen de backTest zetten (anders gaan er onverwachte zaken naar de database enzo)
    static public bool BackTest { get; set; }

    static public SettingsBasic Settings { get; set; } = new();
    static public CryptoApplicationStatus ApplicationStatus { get; set; } = CryptoApplicationStatus.AppStatusPrepare;

    // The nlogger stuff
    static public NLog.Logger Logger { get; } = NLog.LogManager.GetCurrentClassLogger();

    static public List<CryptoInterval> IntervalList { get; } = new();
    static public SortedList<int, CryptoInterval> IntervalListId { get; } = new();
    static public SortedList<CryptoIntervalPeriod, CryptoInterval> IntervalListPeriod { get; } = new();


    // Exchanges indexed on name
    static public SortedList<int, Model.CryptoExchange> ExchangeListId { get; } = new();
    static public SortedList<string, Model.CryptoExchange> ExchangeListName { get; } = new();

    static public Queue<CryptoSignal> SignalQueue { get; } = new();
    static public List<CryptoPosition> PositionsClosed { get; } = new();

    static public event PlayMediaEvent PlaySound;
    static public event PlayMediaEvent PlaySpeech;
    static public event AddTextEvent LogToTelegram;
    static public event AddTextEvent LogToLogTabEvent;

    // Events for refresing data
    static public event AddTextEvent AssetsHaveChangedEvent;
    static public event AddTextEvent SymbolsHaveChangedEvent;
    static public event AddTextEvent PositionsHaveChangedEvent;
    static public event AddTextEvent ConnectionWasLostEvent;
    static public event AddTextEvent ConnectionWasRestoredEvent;

    // Ophalen van historische candles duurt lang, dus niet halverwege nog 1 starten (en nog 1 en...)
    static public event SetCandleTimerEnable SetCandleTimerEnableEvent;

    public static AnalyseEvent SignalEvent { get; set; }

    // TODO: Deze rare accounts proberen te verbergen (indien mogelijk)
    static public SortedList<int, CryptoTradeAccount> TradeAccountList = new();
    static public CryptoTradeAccount ExchangeBackTestAccount { get; set; }
    static public CryptoTradeAccount ExchangeRealTradeAccount { get; set; }
    static public CryptoTradeAccount ExchangePaperTradeAccount { get; set; }


    // Some running tasks/threads
#if SQLDATABASE
    static public ThreadSaveCandles TaskSaveCandles { get; set; }
#endif
    static public ThreadMonitorCandle ThreadMonitorCandle { get; set; }
#if TRADEBOT
    static public ThreadMonitorOrder ThreadMonitorOrder { get; set; }
#endif
#if BALANCING
    static public ThreadBalanceSymbols ThreadBalanceSymbols { get; set; }
#endif

    // Binance stuff
    //static public BinanceStreamUserData TaskBinanceStreamUserData { get; set; }
    //static public BinanceStreamPriceTicker TaskBinanceStreamPriceTicker { get; set; }


    // On special request of a hardcore trader..
    static public SymbolValue FearAndGreedIndex { get; set; } = new();
    static public SymbolValue TradingViewDollarIndex { get; set; } = new();
    static public SymbolValue TradingViewSpx500 { get; set; } = new();
    static public SymbolValue TradingViewBitcoinDominance { get; set; } = new();
    static public SymbolValue TradingViewMarketCapTotal { get; set; } = new();


    static public void LoadExchanges()
    {
        // De exchanges uit de database laden
        ExchangeListId.Clear();
        ExchangeListName.Clear();

        using var database = new CryptoDatabase();
        foreach (Model.CryptoExchange exchange in database.Connection.GetAll<Model.CryptoExchange>())
        {
            AddExchange(exchange);
        }
    }

    static public void LoadAccounts()
    {
        // De accounts uit de database laden
        TradeAccountList.Clear();

        using var database = new CryptoDatabase();
        foreach (CryptoTradeAccount tradeAccount in database.Connection.GetAll<CryptoTradeAccount>())
        {
            TradeAccountList.Add(tradeAccount.Id, tradeAccount);

            // Er zijn 3 accounts altijd aanwezig
            // TODO - enum introduceren vanwege vinkjes ellende, beh
            if (tradeAccount.AccountType == CryptoTradeAccountType.BackTest)
                GlobalData.ExchangeBackTestAccount = tradeAccount;
            if (tradeAccount.AccountType == CryptoTradeAccountType.PaperTrade)
                GlobalData.ExchangePaperTradeAccount = tradeAccount;
            if (tradeAccount.AccountType == CryptoTradeAccountType.RealTrading)
                GlobalData.ExchangeRealTradeAccount = tradeAccount;
        }
    }


    static public void LoadIntervals()
    {
        // De intervallen uit de database laden
        IntervalList.Clear();
        IntervalListId.Clear();
        IntervalListPeriod.Clear();

        using var database = new CryptoDatabase();
        foreach (CryptoInterval interval in database.Connection.GetAll<CryptoInterval>())
        {
            IntervalList.Add(interval);
            IntervalListId.Add(interval.Id, interval);
            IntervalListPeriod.Add(interval.IntervalPeriod, interval);
        }


        // De ContructFrom object koppelen
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (interval.ConstructFromId > 0)
                interval.ConstructFrom = GlobalData.IntervalListId[(int)interval.ConstructFromId];
        }

        // In MSSQL staan ze niet in dej uiste volgorde (vanwege het toevoegen van 2 intervallen)
        IntervalList.Sort((x, y) => x.IntervalPeriod.CompareTo(y.IntervalPeriod));
    }


    static public void AddExchange(Model.CryptoExchange exchange)
    {
        if (!ExchangeListName.ContainsKey(exchange.Name))
        {
            ExchangeListId.Add(exchange.Id, exchange);
            ExchangeListName.Add(exchange.Name, exchange);
        }
    }


    static public CryptoQuoteData AddQuoteData(string quoteName)
    {
        if (!Settings.QuoteCoins.TryGetValue(quoteName, out CryptoQuoteData quoteData))
        {
            quoteData = new CryptoQuoteData
            {
                Name = quoteName,
                DisplayFormat = "N8",
            };

            if (quoteName.Equals("USDT"))
                quoteData.DisplayFormat = "N2";
            if (quoteName.Equals("BUSD"))
                quoteData.DisplayFormat = "N2";

            Settings.QuoteCoins.Add(quoteName, quoteData);
        }
        return quoteData;
    }

    static public void AddSymbol(CryptoSymbol symbol)
    {
        if (ExchangeListId.TryGetValue(symbol.ExchangeId, out Model.CryptoExchange exchange))
        {
            symbol.Exchange = exchange;

            if (!exchange.SymbolListId.ContainsKey(symbol.Id))
                exchange.SymbolListId.Add(symbol.Id, symbol);

            if (!exchange.SymbolListName.ContainsKey(symbol.Name))
                exchange.SymbolListName.Add(symbol.Name, symbol);

            // Een referentie naar de globale quote data opzoeken of aanmaken
            symbol.QuoteData = AddQuoteData(symbol.Quote);


            string s = symbol.PriceTickSize.ToString0();
            int numberOfDecimalPlaces = s.Length - 2;
            symbol.PriceDisplayFormat = "N" + numberOfDecimalPlaces.ToString();

            s = symbol.QuantityTickSize.ToString0();
            numberOfDecimalPlaces = s.Length - 2;
            symbol.QuantityDisplayFormat = "N" + numberOfDecimalPlaces.ToString();
            if (symbol.QuantityTickSize == 1.0m)
                symbol.QuantityDisplayFormat = "N8";
        }
    }


    static public void AddTrade(CryptoTrade trade)
    {
        if (TradeAccountList.TryGetValue(trade.TradeAccountId, out CryptoTradeAccount tradeAccount))
        {
            trade.TradeAccount = tradeAccount;

            if (ExchangeListId.TryGetValue(trade.ExchangeId, out Model.CryptoExchange exchange))
            {
                trade.Exchange = exchange;

                if (exchange.SymbolListId.TryGetValue(trade.SymbolId, out CryptoSymbol symbol))
                {
                    trade.Symbol = symbol;

                    if (!symbol.TradeList.ContainsKey(trade.TradeId))
                    {
                        symbol.TradeList.Add(trade.TradeId, trade);
                    }
                }

            }
        }
    }


    static public void InitBarometerSymbols()
    {
        // TODO: Deze routine is een discrepantie tussen de scanner en trader!
        if (ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
        {
            foreach (CryptoQuoteData quoteData in Settings.QuoteCoins.Values)
            {
                if (quoteData.FetchCandles)
                {
                    if (!exchange.SymbolListName.ContainsKey(Constants.SymbolNameBarometerPrice + quoteData.Name))
                    {
                        CryptoSymbol symbol = new()
                        {
                            Exchange = exchange,
                            ExchangeId = exchange.Id,
                            Base = Constants.SymbolNameBarometerPrice, // De "munt"
                            Quote = quoteData.Name, // USDT, BTC etc.
                            Volume = 0,
                            Status = 1,
                        };
                        symbol.Name = symbol.Base + symbol.Quote;

                        //using var connection = Database.CreateConnection();
                        //using var transaction = connection.BeginTransaction();
                        //connection.Insert(symbol, transaction);
                        //transaction.Commit();
                        AddSymbol(symbol);
                    }
                }
            }
        }
    }


    static public void LoadSettings()
    {
        string filename = GetBaseDir() + "settings.json";
        if (File.Exists(filename))
        {
            //using (FileStream readStream = new FileStream(filename, FileMode.Open))
            //{
            //    BinaryFormatter formatter = new BinaryFormatter();
            //    GlobalData.Settings = (Settings)formatter.Deserialize(readStream);
            //    readStream.Close();
            //}
            string text = File.ReadAllText(filename);
            Settings = JsonSerializer.Deserialize<SettingsBasic>(text);
        }
        else
        {
            // Oude naam = "GlobalData.Settings2.json"
            // Toch de instellingen proberen over te nemen
            string oldSettings = GetBaseDir() + "GlobalData.Settings2.json";
            if (File.Exists(oldSettings))
            {
                try
                {
                    string text = File.ReadAllText(oldSettings);
                    Settings = JsonSerializer.Deserialize<SettingsBasic>(text);
                }
                catch (Exception error)
                {
                    Logger.Error(error);
                    AddTextToLogTab("Error playing music " + error.ToString(), false);
                }
            }
            else 
                DefaultSettings();
        }
    }

    static public void DefaultSettings()
    {
        // Apply some defaults
        if (Settings.QuoteCoins.Count == 0)
        {
            CryptoQuoteData quote = new()
            {
                Name = "BUSD",
                FetchCandles = true,
                CreateSignals = true,
                MinimalVolume = 6500000,
                MinimalPrice = 0.00000001m
            };
            Settings.QuoteCoins.Add(quote.Name, quote);

            quote = new CryptoQuoteData
            {
                Name = "USDT",
                FetchCandles = false,
                CreateSignals = false,
                MinimalVolume = 6500000,
                MinimalPrice = 0.00000001m
            };
            Settings.QuoteCoins.Add(quote.Name, quote);

            quote = new CryptoQuoteData
            {
                Name = "BTC",
                FetchCandles = false,
                CreateSignals = false,
                MinimalVolume = 250,
                MinimalPrice = 0.00000001m
            };
            Settings.QuoteCoins.Add(quote.Name, quote);
        }
    }

    static public void SaveSettings()
    {
        //Laad de gecachte (langere historie, minder overhad)
        string filename = GetBaseDir();
        Directory.CreateDirectory(filename);
        filename += "settings.json";

        //using (FileStream writeStream = new FileStream(filename, FileMode.Create))
        //{
        //    BinaryFormatter formatter = new BinaryFormatter();
        //    formatter.Serialize(writeStream, GlobalData.Settings);
        //    writeStream.Close();
        //}

        string text = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
        //var accountFile = new FileInfo(filename);
        File.WriteAllText(filename, text);

        //filename = GlobalData.GetBaseDir() + "Settings.json";
        //File.WriteAllText(filename, text);
    }


    static public void PlaySomeMusic(string text, bool test = false)
    {
        try
        {
            PlaySound(text, test);
        }
        catch (Exception error)
        {
            Logger.Error(error);
            AddTextToLogTab("Error playing music " + error.ToString(), false);
        }
    }

    static public void PlaySomeSpeech(string text, bool test = false)
    {
        try
        {
            PlaySpeech(text, test);
        }
        catch (Exception error)
        {
            Logger.Error(error);
            AddTextToLogTab("Error playing speech " + error.ToString(), false);
        }
    }

    static public void AddTextToTelegram(string text)
    {
        try
        {
            LogToTelegram(text);
        }
        catch (Exception error)
        {
            Logger.Error(error);
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            AddTextToLogTab(" error telegram thread(1)" + error.ToString(), false);
        }
    }


    static public void AddTextToLogTab(string text, bool extraLineFeed = false)
    {
        LogToLogTabEvent(text, extraLineFeed);
    }


    static public void AssetsHaveChanged(string text)
    {
        AssetsHaveChangedEvent(text);
    }

    static public void SymbolsHaveChanged(string text)
    {
        SymbolsHaveChangedEvent(text);
    }

    static public void PositionsHaveChanged(string text)
    {
        PositionsHaveChangedEvent(text);
    }

    static public void ConnectionWasLost(string text)
    {
        ConnectionWasLostEvent?.Invoke(text);
    }

    static public void ConnectionWasRestored(string text)
    {
        ConnectionWasRestoredEvent?.Invoke(text);
    }

    static public void SetCandleTimerEnable(bool value)
    {
        SetCandleTimerEnableEvent(value);
    }

    static bool IsInitialized = false;


    static public string GetBaseDir()
    {
        if (ApplicationParams.Options == null)
        {
            string[] args = Environment.GetCommandLineArgs();
            ApplicationParams.Options = CommandLine.Parser.Default.ParseArguments<ApplicationParams>(args).Value;
        }

        string folder = ApplicationParams.Options.InputFile;
        if (folder == "")
            folder = "CryptoScanner";

        string baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        folder = Path.Combine(baseFolder, folder);


        if (!IsInitialized)
        {
            IsInitialized = true;
            Directory.CreateDirectory(folder);
        }

        return folder + @"\";
    }

    static public void DebugOnlySymbol(string name)
    {
        // Reduceren naar enkel de BTCUSDT voor traceren van problemen
        Model.CryptoExchange exchangex = GlobalData.ExchangeListId.Values[0];
        for (int i = exchangex.SymbolListId.Values.Count - 1; i >= 0; i--)
        {
            CryptoSymbol symbol = exchangex.SymbolListId.Values[i];
            //De "barometer" munten overslagen AUB
            if (symbol.IsBarometerSymbol())
                continue;
            if (!symbol.Name.Equals(name))
                exchangex.SymbolListId.Remove(symbol.Id);
        }

        // Reduceren naar enkel de BTCUSDT voor traceren van problemen
        for (int i = exchangex.SymbolListName.Values.Count - 1; i >= 0; i--)
        {
            CryptoSymbol symbol = exchangex.SymbolListName.Values[i];
            //De "barometer" munten overslagen AUB
            if (symbol.IsBarometerSymbol())
                continue;
            if (!symbol.Name.Equals(name))
                exchangex.SymbolListName.Remove(symbol.Name);
        }

        SymbolsHaveChangedEvent("");
    }


    static public void InitializeNlog()
    {
        // nlog is lastig te beinvloeden, dus dan maar via code

        /*
        <targets>
            <target name="default" xsi:type="File" 
                fileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner.log" 
                archiveFileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner.{#}.log" 
                archiveEvery="Day" archiveNumbering="Rolling" 
                maxArchiveFiles="7" />
            <target name="errors" xsi:type="File" 
                fileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner-errors.log" 
                archiveFileName="${specialfolder:folder=ApplicationData}/CryptoScanner/CryptoScanner-errors.{#}.log" 
                archiveEvery="Day" 
                archiveNumbering="Rolling" 
                maxArchiveFiles="7" />
        </targets>

		<logger name="*" writeTo="default" />
		<logger name="*" minlevel="Error" writeTo="errors" />
        */

        // Create configuration object 
        var config = new NLog.Config.LoggingConfiguration();

        // Create targets and add them to the configuration 
        var fileTarget = new NLog.Targets.FileTarget();
        fileTarget.Name = "default";
        fileTarget.ArchiveDateFormat = "yyyy-MM-dd";
        fileTarget.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day;
        //fileTarget.EnableArchiveFileCompression = true;
        fileTarget.MaxArchiveDays = 15;
        fileTarget.FileName = GlobalData.GetBaseDir() + "CryptoScanner ${date:format=yyyy-MM-dd} .log";
        //fileTarget.Layout = "Exception Type: ${exception:format=Type}${newline}Target Site:  ${event-context:TargetSite }${newline}Message: ${message}";
        config.AddTarget("file", fileTarget);

        var rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, fileTarget);
        config.LoggingRules.Add(rule);

        fileTarget = new NLog.Targets.FileTarget();
        fileTarget.Name = "errors";
        fileTarget.ArchiveDateFormat = "yyyy-MM-dd";
        fileTarget.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day;
        fileTarget.MaxArchiveDays = 30;
        //fileTarget.EnableArchiveFileCompression = true;
        fileTarget.FileName = GlobalData.GetBaseDir() + "CryptoScanner ${date:format=yyyy-MM-dd}-Errors.log";
        //fileTarget.Layout = "Exception Type: ${exception:format=Type}${newline}Target Site:  ${event-context:TargetSite }${newline}Message: ${message}";
        config.AddTarget("file", fileTarget);

        rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Error, fileTarget);
        config.LoggingRules.Add(rule);


        NLog.LogManager.Configuration = config;
    }
}
