namespace Application.Common.Interfaces;

public interface IExcelImportService
{
    /// <summary>
    /// Reads the "Companies Import" sheet (rows 5+) from the uploaded xlsx stream.
    /// Returns raw row data — validation happens in the handler.
    /// </summary>
    IEnumerable<(int RowNumber, Dictionary<string, string?> Cells)> ReadCompanyRows(Stream stream);
}
