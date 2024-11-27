// Copyright (C) 2024 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using Refactored.Waffle.ExcelWriter.Attributes;

namespace Refactored.Waffle.ExcelWriter.Services;

internal sealed class ExcelWriterService(ILogger<ExcelWriterService> logger) : IExcelWriterService
{
    public void CreateExcel<TData>(IReadOnlyList<TData> data, string filePath, string sheetName = "Sheet1")
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        var properties = typeof(TData).GetProperties();

        WriteHeaders(worksheet, properties);
        WriteData(worksheet, data, properties);

        workbook.SaveAs(filePath);
        logger.LogDebug("Excel file created successfully at: {FilePath}", filePath);
    }

    private void WriteHeaders(IXLWorksheet worksheet, PropertyInfo[] properties)
    {
        for (var col = 1; col <= properties.Length; col++)
        {
            var property = properties[col - 1];

            var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null)
            {
                var columnName = displayAttribute.Name;
                worksheet.Cell(1, col).Value = columnName;
            }
            else
            {
                worksheet.Cell(1, col).Value = property.Name;
            }

            var widthAttribute = property.GetCustomAttribute<ExcelColumnWidthAttribute>();
            if (widthAttribute != null)
            {
                worksheet.Column(col).Width = widthAttribute.Width;
            }
            worksheet.Cell(1, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.CenterContinuous;
            worksheet.Cell(1, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Cell(1, col).Style.Alignment.WrapText = true;
        }

        logger.LogDebug("Headers written to the Excel worksheet.");
    }

    private void WriteData<TData>(
        IXLWorksheet worksheet, IReadOnlyList<TData> data, IReadOnlyList<PropertyInfo> properties)
    {
        var columnsToDelete = new HashSet<int>();

        for (var col = 1; col <= properties.Count; col++)
        {
            var allValuesAreDash = true;

            for (var row = 1; row <= data.Count; row++)
            {
                var item = data[row - 1];
                var property = properties[col - 1];

                var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
                object value;

                if (displayAttribute != null)
                {
                    var columnIndex = displayAttribute.Order;
                    value = property.GetValue(item) ?? "--";
                    SetCellValue(worksheet, row + 1, columnIndex, value);
                }
                else
                {
                    value = property.GetValue(item) ?? "--";
                    SetCellValue(worksheet, row + 1, col, value);
                }

                if (value.ToString() != "--")
                {
                    allValuesAreDash = false;
                }

                worksheet.Cell(row + 1, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            if (allValuesAreDash)
            {
                columnsToDelete.Add(col);
            }
        }

        foreach (var col in columnsToDelete.Reverse())
        {
            worksheet.Column(col).Delete();
        }

        logger.LogDebug("Data written to the Excel worksheet for {Count} rows.", data.Count);
    }

    private void SetCellValue(IXLWorksheet worksheet, int row, int column, object? value)
    {
        if (value != null)
        {
            switch (value)
            {
                case DateTime dateTimeValue:
                    worksheet.Cell(row, column).Value = dateTimeValue;
                    break;
                case DateTimeOffset dateTimeOffsetValue:
                    worksheet.Cell(row, column).Value = dateTimeOffsetValue.DateTime;
                    break;
                case Guid guidValue:
                    worksheet.Cell(row, column).Value = guidValue.ToString();
                    break;
                default:
                    {
                        if (IsNumericType(value))
                        {
                            worksheet.Cell(row, column).Value = Convert.ToDouble(value);
                        }
                        else
                        {
                            worksheet.Cell(row, column).Value = value.ToString();
                        }

                        break;
                    }
            }
        }
        else
        {
            worksheet.Cell(row, column).Value = string.Empty;
        }
    }

    private static bool IsNumericType(object value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }
}