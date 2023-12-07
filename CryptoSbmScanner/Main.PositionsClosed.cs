﻿using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Trader;

namespace CryptoSbmScanner;

#if TRADEBOT

public partial class FrmMain
{
    private ContextMenuStrip ContextMenuStripPositionsClosed;
    private ListViewHeaderContext listViewPositionsClosed;

    // Commands
    private ToolStripMenuItem CommandPositionsClosedExcelDump;
    private ToolStripMenuItem CommandPositionsClosedRecalculate;
    private ToolStripMenuItem CommandActivateTradingAppPositionsClosed;
    private ToolStripMenuItem CommandPositionsClosedActivateTradingViewInternal;
    private ToolStripMenuItem CommandPositionsClosedActivateTradingViewExternal;

    private void ListViewPositionsClosedConstructor()
    {
        ContextMenuStripPositionsClosed = new ContextMenuStrip();

        // Commands
        CommandActivateTradingAppPositionsClosed = new ToolStripMenuItem();
        CommandActivateTradingAppPositionsClosed.Text = "Activate trading app";
        CommandActivateTradingAppPositionsClosed.Tag = Command.ActivateTradingApp;
        CommandActivateTradingAppPositionsClosed.Click += Commands.ExecuteCommandCommandViaTag;
        ContextMenuStripPositionsClosed.Items.Add(CommandActivateTradingAppPositionsClosed);

        CommandPositionsClosedActivateTradingViewInternal = new ToolStripMenuItem();
        CommandPositionsClosedActivateTradingViewInternal.Text = "TradingView browser";
        CommandPositionsClosedActivateTradingViewInternal.Tag = Command.ActivateTradingviewIntern;
        CommandPositionsClosedActivateTradingViewInternal.Click += Commands.ExecuteCommandCommandViaTag;
        ContextMenuStripPositionsClosed.Items.Add(CommandPositionsClosedActivateTradingViewInternal);

        CommandPositionsClosedActivateTradingViewExternal = new ToolStripMenuItem();
        CommandPositionsClosedActivateTradingViewExternal.Text = "TradingView extern";
        CommandPositionsClosedActivateTradingViewExternal.Tag = Command.ActivateTradingviewExtern;
        CommandPositionsClosedActivateTradingViewExternal.Click += Commands.ExecuteCommandCommandViaTag;
        ContextMenuStripPositionsClosed.Items.Add(CommandPositionsClosedActivateTradingViewExternal);

        ContextMenuStripPositionsClosed.Items.Add(new ToolStripSeparator());

        CommandPositionsClosedRecalculate = new ToolStripMenuItem();
        CommandPositionsClosedRecalculate.Text = "Herberekenen";
        CommandPositionsClosedRecalculate.Click += CommandPositionsClosedRecalculateExecute;
        ContextMenuStripPositionsClosed.Items.Add(CommandPositionsClosedRecalculate);

        CommandPositionsClosedExcelDump = new ToolStripMenuItem();
        CommandPositionsClosedExcelDump.Text = "Positie informatie (Excel)";
        CommandPositionsClosedExcelDump.Tag = Command.ExcelPositionInformation;
        CommandPositionsClosedExcelDump.Click += Commands.ExecuteCommandCommandViaTag;
        ContextMenuStripPositionsClosed.Items.Add(CommandPositionsClosedExcelDump);


        // ruzie (component of events raken weg), dan maar dynamisch
        listViewPositionsClosed = new()
        {
            Dock = DockStyle.Fill,
            Location = new Point(4, 3)
        };
        listViewPositionsClosed.ColumnClick += ListViewPositionsClosedColumnClick;

        listViewPositionsClosed.Tag = Command.ActivateTradingApp;
        listViewPositionsClosed.DoubleClick += Commands.ExecuteCommandCommandViaTag;
        //listViewPositionsClosed.DoubleClick += CommandActivateTradingAppPositionsClosedExecute;
        tabPagePositionsClosed.Controls.Add(listViewPositionsClosed);

        listViewPositionsClosed.ContextMenuStrip = ContextMenuStripPositionsClosed;

        listViewPositionsClosed.ListViewItemSorter = new ListViewColumnSorterPosition()
        {
            SortColumn = 2,
            ClosedPositions = true,
            SortOrder = SortOrder.Descending
        }; ;

        //TimerClearEvents = new();
        //InitTimerInterval(ref TimerClearEvents, 1 * 60);
        //TimerClearEvents.Tick += TimerClearOldSignals_Tick;

        ListViewPositionsClosedInitColumns();
    }

    private void ListViewPositionsClosedColumnClick(object sender, ColumnClickEventArgs e)
    {
        listViewPositionsClosed.BeginUpdate();
        try
        {
            // Perform the sort with these new sort options.
            ListViewColumnSorterPosition listViewColumnSorter = (ListViewColumnSorterPosition)listViewPositionsClosed.ListViewItemSorter;
            listViewColumnSorter.ClickedOnColumn(e.Column);
            listViewPositionsClosed.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.SortOrder);
            listViewPositionsClosed.Sort();
        }
        finally
        {
            listViewPositionsClosed.EndUpdate();
        }
    }



    private void ListViewPositionsClosedInitColumns()
    {
        // TODO: Positie kolommen kiezen..

        // Create columns and subitems. Width of -2 indicates auto-size
        listViewPositionsClosed.Columns.Add("ID", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Datum", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Closed", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Duration", -2, HorizontalAlignment.Right);
        listViewPositionsClosed.Columns.Add("Account", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Exchange", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Symbol", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Interval", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Strategie", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Mode", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Status", -2, HorizontalAlignment.Left);

        //listViewPositionsClosed.Columns.Add("Quantity", -2, HorizontalAlignment.Right);
        //listViewPositionsClosed.Columns.Add("Price", -2, HorizontalAlignment.Right);
        //listViewPositionsClosed.Columns.Add("BreakEven", -2, HorizontalAlignment.Right);

        listViewPositionsClosed.Columns.Add("Invested", -2, HorizontalAlignment.Right);
        listViewPositionsClosed.Columns.Add("Returned", -2, HorizontalAlignment.Right);
        listViewPositionsClosed.Columns.Add("Commission", -2, HorizontalAlignment.Right);

        listViewPositionsClosed.Columns.Add("Profit", -2, HorizontalAlignment.Right);
        listViewPositionsClosed.Columns.Add("Percentage", -2, HorizontalAlignment.Right);

        listViewPositionsClosed.Columns.Add("Parts", -2, HorizontalAlignment.Right);
        listViewPositionsClosed.Columns.Add("BuyPrice", -2, HorizontalAlignment.Right);
        listViewPositionsClosed.Columns.Add("SellPrice", -2, HorizontalAlignment.Right);

        listViewPositionsClosed.Columns.Add("", -2, HorizontalAlignment.Right); // filler

        listViewPositionsClosed.SetSortIcon(
              ((ListViewColumnSorterPosition)listViewPositionsClosed.ListViewItemSorter).SortColumn,
              ((ListViewColumnSorterPosition)listViewPositionsClosed.ListViewItemSorter).SortOrder);

        for (int i = 0; i <= listViewPositionsClosed.Columns.Count - 1; i++)
        {
            //if (i != 5)
                listViewPositionsClosed.Columns[i].Width = -2;
        }
    }


    private static void FillItemClosed(CryptoPosition position, ListViewItem item1)
    {
        // Omdat het item via een range wordt toegevoegd is deze niet beschikbaar
        //if (item1.Index % 2 == 0)
        //    item1.BackColor = Color.LightGray;

        ListViewItem.ListViewSubItem subItem;
        item1.SubItems.Clear();

        item1.Text = position.Id.ToString();
        item1.SubItems.Add(position.CreateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        item1.SubItems.Add(position.CloseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        item1.SubItems.Add(position.DurationText());
        item1.SubItems.Add(position.TradeAccount.Name);
        item1.SubItems.Add(position.Symbol.Exchange.Name);
        item1.SubItems.Add(position.Symbol.Name);
        item1.SubItems.Add(position.Interval.Name);
        item1.SubItems.Add(position.StrategyText);

        subItem = item1.SubItems.Add(position.SideText);
        if (position.Side == CryptoTradeSide.Long)
            subItem.ForeColor = Color.Green;
        else if (position.Side == CryptoTradeSide.Short)
            subItem.ForeColor = Color.Red;

        subItem = item1.SubItems.Add(position.Status.ToString());
        if (position.Status == CryptoPositionStatus.Waiting)
            subItem.ForeColor = Color.Red;

        //item1.SubItems.Add(position.Quantity.ToString0("N8"));
        //item1.SubItems.Add(position.BuyPrice.ToString(position.Symbol.DisplayFormat));
        //item1.SubItems.Add(position.BreakEvenPrice.ToString(position.Symbol.DisplayFormat));

        item1.SubItems.Add(position.Invested.ToString(position.Symbol.QuoteData.DisplayFormat));
        item1.SubItems.Add(position.Returned.ToString(position.Symbol.QuoteData.DisplayFormat));
        item1.SubItems.Add(position.Commission.ToString(position.Symbol.QuoteData.DisplayFormat));

        // Profit bedrag
        subItem = item1.SubItems.Add(position.Profit.ToString(position.Symbol.QuoteData.DisplayFormat));
        if (position.Percentage > 100)
            subItem.ForeColor = Color.Green;
        else if (position.Percentage < 100)
            subItem.ForeColor = Color.Red;

        // Profit percentage
        subItem = item1.SubItems.Add(position.Percentage.ToString("N2"));
        if (position.Percentage > 100)
            subItem.ForeColor = Color.Green;
        else if (position.Percentage < 100)
            subItem.ForeColor = Color.Red;

        item1.SubItems.Add(position.PartCount.ToString());
        item1.SubItems.Add(position.BuyPrice?.ToString(position.Symbol.PriceDisplayFormat));
        item1.SubItems.Add(position.SellPrice?.ToString(position.Symbol.PriceDisplayFormat));
    }

    private static ListViewItem AddClosedPosition(CryptoPosition position)
    {
        ListViewItem item = new("", -1)
        {
            UseItemStyleForSubItems = false
        };
        FillItemClosed(position, item);

        return item;
    }


    private void ClosedPositionsHaveChangedEvent()
    {
        if (IsHandleCreated) // && (!ProgramExit) &&  components != null && 
        {
            // Gesloten posities
            Task.Run(() => {
                Invoke(new Action(() =>
                {
                    ListViewPositionsClosedAddPositions(GlobalData.PositionsClosed);
                }));
            });
        }
    }


    private void ListViewPositionsClosedAddPositions(List<CryptoPosition> list)
    {
        listViewPositionsClosed.BeginUpdate();
        try
        {
            List<ListViewItem> range = new();
            foreach (CryptoPosition position in list.ToList())
            {
                ListViewItem item = AddClosedPosition(position);
                item.Tag = position;
                range.Add(item);
            }

            listViewPositionsClosed.Clear();
            ListViewPositionsClosedInitColumns();
            listViewPositionsClosed.Items.AddRange(range.ToArray());

            // Deze redelijk kostbaar? (alles moet gecontroleerd worden)
            for (int i = 0; i <= listViewPositionsClosed.Columns.Count - 1; i++)
            {
                listViewPositionsClosed.Columns[i].Width = -2;
            }
        }
        finally
        {
            listViewPositionsClosed.EndUpdate();
        }
    }


    private void ListViewPositionsClosedInitCaptions()
    {
        string text = GlobalData.ExternalUrls.GetTradingAppName(GlobalData.Settings.General.TradingApp, GlobalData.Settings.General.ExchangeName);
        CommandActivateTradingAppPositionsClosed.Text = text;
    }

    private async void CommandPositionsClosedRecalculateExecute(object sender, EventArgs e)
    {
        if (listViewPositionsClosed.SelectedItems.Count > 0)
        {
            ListViewItem item = listViewPositionsClosed.SelectedItems[0];
            CryptoPosition position = (CryptoPosition)item.Tag;

            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
            TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);
            FillItemClosed(position, item);
        }

    }

}
#endif

