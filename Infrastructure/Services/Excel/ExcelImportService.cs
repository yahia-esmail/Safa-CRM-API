using Application.Common.Interfaces;
using ClosedXML.Excel;

namespace Infrastructure.Services.Excel;

public class ExcelImportService : IExcelImportService
{
    private const string SheetName  = "Companies Import";
    private const int    DataStartRow = 5;   // row 1–4 = header/instructions

    // Column letter → field name mapping
    private static readonly Dictionary<string, string> ColumnMap = new()
    {
        ["A"] = "EnglishName",
        ["B"] = "ArabicName",
        ["C"] = "Country",
        ["D"] = "Phone",
        ["E"] = "Email",
        ["F"] = "Website",
        ["G"] = "SafaKey",
        ["H"] = "AccountType",
        ["I"] = "Stage",
        ["J"] = "LeadSource",
        ["K"] = "LeadStatus",
        ["L"] = "ExpectedRevenue",
        ["M"] = "AssignedTo",
        ["N"] = "ContractAttachment",
        ["O"] = "ApplicationForm",
        ["P"] = "Notes",
    };

    public IEnumerable<(int RowNumber, Dictionary<string, string?> Cells)> ReadCompanyRows(Stream stream)
    {
        using var wb = new XLWorkbook(stream);

        if (!wb.TryGetWorksheet(SheetName, out var ws))
            throw new InvalidOperationException($"Sheet '{SheetName}' not found in the uploaded file. Please use the official template.");

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? DataStartRow - 1;

        for (int r = DataStartRow; r <= lastRow; r++)
        {
            var row = ws.Row(r);

            // Skip completely empty rows
            if (ColumnMap.Keys.All(col => string.IsNullOrWhiteSpace(row.Cell(col).GetString())))
                continue;

            var cells = new Dictionary<string, string?>();
            foreach (var (col, field) in ColumnMap)
            {
                var raw = row.Cell(col).GetString()?.Trim();
                cells[field] = string.IsNullOrWhiteSpace(raw) ? null : raw;
            }

            yield return (r, cells);
        }
    }
}
