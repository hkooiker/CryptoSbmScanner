﻿using CryptoSbmScanner.Model;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace CryptoSbmScanner.Intern;

public class CandleDumpDebug
{
    public static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, string value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.SetCellValue(value);
        return cell;
    }

    public static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, double value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.SetCellValue(value);
        return cell;
    }

    public static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, DateTime value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.SetCellValue(value);
        return cell;
    }

    public static ICell WriteStyle(ISheet sheet, int columnIndex, int rowIndex, ICellStyle style)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        ICell cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.CellStyle = style;
        return cell;
    }

    private static int ExcellHeaders(HSSFSheet sheet, int row)
    {
        int column = 0;

        // Columns...
        WriteCell(sheet, column++, row, "Date");
        WriteCell(sheet, column++, row, "Open");
        WriteCell(sheet, column++, row, "High");
        WriteCell(sheet, column++, row, "Low");
        WriteCell(sheet, column++, row, "Close");
        WriteCell(sheet, column++, row, "Volume");

        return column;
    }

    public static string ExportToExcell(CryptoSymbol symbol)
    {
        // HSSF => Microsoft Excel(excel 97-2003)
        // XSSF => Office Open XML Workbook(excel 2007)
        HSSFWorkbook book = new();

        //create a entry of DocumentSummaryInformation
        DocumentSummaryInformation documentSummaryInformation = PropertySetFactory.CreateDocumentSummaryInformation();
        documentSummaryInformation.Company = "Crypto Scanner";
        book.DocumentSummaryInformation = documentSummaryInformation;

        //create a entry of SummaryInformation
        SummaryInformation summaryInformation = PropertySetFactory.CreateSummaryInformation();
        summaryInformation.Subject = symbol.Name;
        book.SummaryInformation = summaryInformation;

        IDataFormat format = book.CreateDataFormat();


        ICellStyle cellStyleDate = book.CreateCellStyle();
        cellStyleDate.DataFormat = format.GetFormat("dd-MM-yyyy HH:mm");
        cellStyleDate.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;

        //ICellStyle cellStyleStringGreen = book.CreateCellStyle();
        //cellStyleStringGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        //cellStyleStringGreen.FillPattern = FillPattern.SolidForeground;
        //cellStyleStringGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

        ICellStyle cellStyleDecimalNormal = book.CreateCellStyle();
        cellStyleDecimalNormal.DataFormat = format.GetFormat("0.00000000");

        //ICellStyle cellStyleDecimalGreen = book.CreateCellStyle();
        //cellStyleDecimalGreen.DataFormat = format.GetFormat("0.00000000");
        //cellStyleDecimalGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        //cellStyleDecimalGreen.FillPattern = FillPattern.SolidForeground;
        //cellStyleDecimalGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

        //ICellStyle cellStyleDecimalRed = book.CreateCellStyle();
        //cellStyleDecimalRed.DataFormat = format.GetFormat("0.00000000");
        //cellStyleDecimalRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
        //cellStyleDecimalRed.FillPattern = FillPattern.SolidForeground;
        //cellStyleDecimalRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;


        //ICellStyle cellStylePercentageNormal = book.CreateCellStyle();
        //cellStylePercentageNormal.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");

        //ICellStyle cellStylePercentageGreen = book.CreateCellStyle();
        //cellStylePercentageGreen.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
        //cellStylePercentageGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        //cellStylePercentageGreen.FillPattern = FillPattern.SolidForeground;
        //cellStylePercentageGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;


        //// macd.red
        //ICellStyle cellStyleMacdRed = book.CreateCellStyle();
        //cellStyleMacdRed.DataFormat = format.GetFormat("0.00000000");
        //cellStyleMacdRed.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        //cellStyleMacdRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
        //cellStyleMacdRed.FillPattern = FillPattern.SolidForeground;
        //cellStyleMacdRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Lime.Index;

        //// macd.roze
        //ICellStyle cellStyleMacdLightRed = book.CreateCellStyle();
        //cellStyleMacdLightRed.DataFormat = format.GetFormat("0.00000000");
        //cellStyleMacdLightRed.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        //cellStyleMacdLightRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Rose.Index;
        //cellStyleMacdLightRed.FillPattern = FillPattern.SolidForeground;
        //cellStyleMacdLightRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Rose.Index;

        //// macd.green
        //ICellStyle cellStyleMacdGreen = book.CreateCellStyle();
        //cellStyleMacdGreen.DataFormat = format.GetFormat("0.00000000");
        //cellStyleMacdGreen.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        //cellStyleMacdGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Green.Index;
        //cellStyleMacdGreen.FillPattern = FillPattern.SolidForeground;
        //cellStyleMacdGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Green.Index;

        //// macd.ligh green
        //ICellStyle cellStyleMacdLightGreen = book.CreateCellStyle();
        //cellStyleMacdLightGreen.DataFormat = format.GetFormat("0.00000000");
        //cellStyleMacdLightGreen.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        //cellStyleMacdLightGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        //cellStyleMacdLightGreen.FillPattern = FillPattern.SolidForeground;
        //cellStyleMacdLightGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;



        foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList.ToList())
        {
            HSSFSheet sheet = (HSSFSheet)book.CreateSheet(symbolInterval.Interval.Name);

            int row = 0;
            int column;
            int columns = ExcellHeaders(sheet, 0);

            foreach (CryptoCandle candle in symbolInterval.CandleList.Values.ToList())
            {
                row++;
                column = 0;

                ICell cell;
                cell = WriteCell(sheet, column++, row, candle.DateLocal);
                cell.CellStyle = cellStyleDate;

                cell = WriteCell(sheet, column++, row, (double)candle.Open);
                cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.High);
                cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.Low);
                cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.Close);
                cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.Volume);
                cell.CellStyle = cellStyleDecimalNormal;
            }

            // wel makkelijk als ze ook onderin staan
            //ExcellHeaders(sheet, row + 1);

            for (int i = 0; i < columns; i++)
            {
                sheet.AutoSizeColumn(i);
                int width = sheet.GetColumnWidth(i);
                sheet.SetColumnWidth(i, (int)(1.1 * width));
            }
        }



        GlobalData.AddTextToLogTab($"Debug dump {symbol.Name} ready");

        string folder = GlobalData.GetBaseDir() + $@"\Debug\";
        Directory.CreateDirectory(folder);

        string filename = folder + symbol.Name + ".xls";
        using var fs = new FileStream(filename, FileMode.Create);
        book.Write(fs);

        return filename;
    }
}