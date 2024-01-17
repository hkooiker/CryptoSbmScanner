﻿using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Signal;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;


/// <summary>
/// Een position is 1 een samenvatting van 1 of meerdere orders
/// </summary>
[Table("Position")]
public class CryptoPosition
{
    [Key]
    public int Id { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public DateTime? CloseTime { get; set; }

    public int TradeAccountId { get; set; }
    [Computed]
    public CryptoTradeAccount TradeAccount { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public CryptoExchange Exchange { get; set; }

    public int SymbolId { get; set; }
    [Computed]
    public CryptoSymbol Symbol { get; set; }

    public int? IntervalId { get; set; }
    [Computed]
    public CryptoInterval Interval { get; set; }

    public CryptoTradeSide Side { get; set; }
    [Computed]
    public string SideText { get { return Side switch { CryptoTradeSide.Long => "long", CryptoTradeSide.Short => "short", _ => "?", }; } }

    [Computed]
    public string DisplayText { get { return Symbol.Name + " " + Interval.Name + " " + CreateTime.ToLocalTime() + " " + SideText + " " + StrategyText; } }

    public CryptoSignalStrategy Strategy { get; set; }
    [Computed]
    public string StrategyText { get { return SignalHelper.GetSignalAlgorithmText(Strategy); } }

    // Globale status van de positie (new, closed, wellicht andere enum?)
    public CryptoPositionStatus? Status { get; set; }

    public decimal Invested { get; set; }
    public decimal Returned { get; set; }
    public decimal Commission { get; set; }
    public decimal Profit { get; set; }
    public decimal Percentage { get; set; }

    public decimal Quantity { get; set; }
    public decimal BreakEvenPrice { get; set; }

    // Hulpmiddelen voor statistiek en dca (niet noodzakelijk)
    public decimal? EntryPrice { get; set; }
    public decimal? EntryAmount { get; set; }
    public decimal? ProfitPrice { get; set; }

    // Een experiment (die weg kan, we zetten er nu even de naam van de munt in, handig)
    public string Data { get; set; }

    // Soort van Parts.Count (maar dan hoeft niet alles geladen te zijn)
    public int PartCount { get; set; }
    [Computed]
    public bool ActiveDca { get; set; }
    
    // Zou computed kunnen, maar voor de zekerheid in de database
    public bool Reposition { get; set; }

    [Computed]
    public SortedList<int, CryptoPositionPart> Parts { get; set; } = new();

    [Computed]
    // Orders die uitstaan via de parts/steps
    public SortedList<string, CryptoPositionStep> Orders { get; set; } = new();
}


public static class CryptoPositionHelper
{
    /// <summary>
    /// Netto winst (als je nu zou verkopen)
    /// </summary>
    public static decimal CurrentProfit(this CryptoPosition position)
    {
        if (!position.Symbol.LastPrice.HasValue)
            return 0m;
        else
        {
            decimal profit = position.Quantity * position.Symbol.LastPrice.Value;
            if (position.Side == CryptoTradeSide.Long)
                profit = profit - position.Invested - position.Returned;
            else
                profit = position.Invested - position.Returned - profit;
            profit -= position.Commission;

            return profit;
        }
    }

    /// <summary>
    /// Netto waarde (als je nu zou verkopen)
    /// </summary>
    public static decimal CurrentValue(this CryptoPosition position)
    {
        if (!position.Symbol.LastPrice.HasValue)
            return 0m;
        else
        {
            decimal profit = position.Quantity * position.Symbol.LastPrice.Value;
            if (position.Side == CryptoTradeSide.Long)
                profit = profit - position.Invested - position.Returned;
            else
                profit = position.Invested - position.Returned - profit;
            profit -= position.Commission;

            return position.Invested - position.Returned + profit;
        }
    }

    /// <summary>
    /// Winst percentage (als je nu zou verkopen)
    /// </summary>
    public static decimal CurrentProfitPercentage(this CryptoPosition position)
    {
        decimal total = position.Invested - position.Returned;
        if (total == 0)
            return 0m;
        else 
            return 100 * position.CurrentProfit() / total;
    }


    public static TimeSpan Duration(this CryptoPosition position)
    {
        TimeSpan span;
        if (position.CloseTime.HasValue)
            span = (DateTime)position.CloseTime - position.CreateTime;
        else
            span = DateTime.UtcNow - position.CreateTime;
        return span;
    }


    public static string DurationText(this CryptoPosition position)
    {
        TimeSpan span = position.Duration();

        string text = "";
        if (span.Days > 0)
            text += $"{span.Days}d";
        if (span.Hours > 0)
            text += $" {span.Hours}h";
        if (span.Minutes > 0)
            text += $" {span.Minutes}m";
        //if (span.Seconds > 0)
        //    text += $" {span.Seconds}s";
        return text.Trim();
    }


    public static CryptoOrderSide GetEntryOrderSide(this CryptoPosition position)
    {
        if (position.Side == CryptoTradeSide.Long)
            return CryptoOrderSide.Buy;
        else
            return CryptoOrderSide.Sell;
    }


    public static CryptoOrderSide GetTakeProfitOrderSide(this CryptoPosition position)
    {
        if (position.Side == CryptoTradeSide.Long)
            return CryptoOrderSide.Sell;
        else
            return CryptoOrderSide.Buy;
    }

}
