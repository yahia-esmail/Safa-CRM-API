using Application.Common.Interfaces;
using Application.Features.Dashboard;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.Services.Excel;

public class ExportService : IExportService
{
    public ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportCompanies(IEnumerable<CompanyExportRow> companies)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Companies");
        
        // Headers
        worksheet.Cell(1, 1).Value = "English Name";
        worksheet.Cell(1, 2).Value = "Arabic Name";
        worksheet.Cell(1, 3).Value = "Country";
        worksheet.Cell(1, 4).Value = "Phone";
        worksheet.Cell(1, 5).Value = "Email";
        worksheet.Cell(1, 6).Value = "Stage";
        worksheet.Cell(1, 7).Value = "Account Type";
        worksheet.Cell(1, 8).Value = "Assigned To";
        worksheet.Cell(1, 9).Value = "Created At";
        worksheet.Cell(1, 10).Value = "Last Activity";

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
        headerRow.Style.Font.FontColor = XLColor.White;

        int rowNum = 2;
        foreach (var c in companies)
        {
            worksheet.Cell(rowNum, 1).Value = c.EnglishName;
            worksheet.Cell(rowNum, 2).Value = c.ArabicName;
            worksheet.Cell(rowNum, 3).Value = c.Country;
            worksheet.Cell(rowNum, 4).Value = c.Phone;
            worksheet.Cell(rowNum, 5).Value = c.Email;
            worksheet.Cell(rowNum, 6).Value = c.Stage;
            worksheet.Cell(rowNum, 7).Value = c.AccountType;
            worksheet.Cell(rowNum, 8).Value = c.AssignedTo ?? "";
            worksheet.Cell(rowNum, 9).Value = c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cell(rowNum, 10).Value = c.LastActivity?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
            rowNum++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportOrders(IEnumerable<OrderExportRow> orders)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Orders");

        // Headers
        worksheet.Cell(1, 1).Value = "Invoice Number";
        worksheet.Cell(1, 2).Value = "Company";
        worksheet.Cell(1, 3).Value = "Order Type";
        worksheet.Cell(1, 4).Value = "Status";
        worksheet.Cell(1, 5).Value = "Currency";
        worksheet.Cell(1, 6).Value = "Original Amount";
        worksheet.Cell(1, 7).Value = "Usd Amount";
        worksheet.Cell(1, 8).Value = "Created By";
        worksheet.Cell(1, 9).Value = "Created At";

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
        headerRow.Style.Font.FontColor = XLColor.White;

        int rowNum = 2;
        foreach (var o in orders)
        {
            worksheet.Cell(rowNum, 1).Value = o.InvoiceNumber;
            worksheet.Cell(rowNum, 2).Value = o.Company;
            worksheet.Cell(rowNum, 3).Value = o.OrderType;
            worksheet.Cell(rowNum, 4).Value = o.Status;
            worksheet.Cell(rowNum, 5).Value = o.Currency;
            worksheet.Cell(rowNum, 6).Value = (double)o.OriginalAmount;
            worksheet.Cell(rowNum, 7).Value = (double)o.UsdAmount;
            worksheet.Cell(rowNum, 8).Value = o.CreatedBy;
            worksheet.Cell(rowNum, 9).Value = o.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            rowNum++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] GenerateDashboardPdf(AdminDashboardDto adminData, DateTime from, DateTime to)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeDashboardHeader(c, "Admin Dashboard Report", from, to));
                page.Content().Element(c => ComposeAdminDashboardContent(c, adminData));
                page.Footer().Element(ComposeFooter);
            });
        });
        return doc.GeneratePdf();
    }

    public byte[] GenerateDashboardPdf(SalesDashboardDto salesData, string repName, DateTime from, DateTime to)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeDashboardHeader(c, $"Sales Report: {repName}", from, to));
                page.Content().Element(c => ComposeSalesDashboardContent(c, salesData));
                page.Footer().Element(ComposeFooter);
            });
        });
        return doc.GeneratePdf();
    }

    private void ComposeDashboardHeader(IContainer container, string title, DateTime from, DateTime to)
    {
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(title).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Period: {from:dd MMM yyyy} to {to:dd MMM yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
            });
            row.ConstantItem(150).AlignRight().Column(column =>
            {
                column.Item().Text("SAFA CRM SYSTEM").FontSize(11).Bold();
                column.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeAdminDashboardContent(IContainer container, AdminDashboardDto data)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Spacing(15);

            // KPI Cards Row
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => Card(c, "Total Companies", data.TotalActiveCompanies.ToString(), Colors.Blue.Lighten5, Colors.Blue.Darken2));
                row.Spacing(10);
                row.RelativeItem().Element(c => Card(c, "Total Orders", data.TotalOrders.ToString(), Colors.Green.Lighten5, Colors.Green.Darken2));
                row.Spacing(10);
                row.RelativeItem().Element(c => Card(c, "Total Sales USD", $"${data.TotalSalesUsd:N2}", Colors.Amber.Lighten5, Colors.Amber.Darken3));
            });

            // Sales By Country
            column.Item().Text("Sales By Country").Bold().FontSize(12);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                table.Header(h =>
                {
                    h.Cell().Element(HeaderStyle).Text("Country");
                    h.Cell().Element(HeaderStyle).Text("Orders");
                    h.Cell().Element(HeaderStyle).AlignRight().Text("USD Amount");
                });
                foreach (var x in data.SalesByCountry)
                {
                    table.Cell().Element(RowStyle).Text(x.Country);
                    table.Cell().Element(RowStyle).Text(x.OrderCount.ToString());
                    table.Cell().Element(RowStyle).AlignRight().Text($"${x.UsdAmount:N2}");
                }
            });

            // Sales By Representative
            column.Item().Text("Sales By Representative").Bold().FontSize(12);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                table.Header(h =>
                {
                    h.Cell().Element(HeaderStyle).Text("Representative");
                    h.Cell().Element(HeaderStyle).Text("Orders");
                    h.Cell().Element(HeaderStyle).AlignRight().Text("USD Amount");
                });
                foreach (var x in data.SalesByRep)
                {
                    table.Cell().Element(RowStyle).Text(x.RepName);
                    table.Cell().Element(RowStyle).Text(x.OrderCount.ToString());
                    table.Cell().Element(RowStyle).AlignRight().Text($"${x.UsdAmount:N2}");
                }
            });

            // Top Solutions
            column.Item().Text("Top Technical Solutions").Bold().FontSize(12);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                table.Header(h =>
                {
                    h.Cell().Element(HeaderStyle).Text("Solution");
                    h.Cell().Element(HeaderStyle).Text("Items Confirmed");
                    h.Cell().Element(HeaderStyle).AlignRight().Text("USD Revenue");
                });
                foreach (var x in data.TopSolutions)
                {
                    table.Cell().Element(RowStyle).Text(x.Solution);
                    table.Cell().Element(RowStyle).Text(x.Count.ToString());
                    table.Cell().Element(RowStyle).AlignRight().Text($"${x.UsdAmount:N2}");
                }
            });
        });

        static IContainer HeaderStyle(IContainer container) => container.Background(Colors.Grey.Lighten3).Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Medium);
        static IContainer RowStyle(IContainer container) => container.Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
    }

    private void ComposeSalesDashboardContent(IContainer container, SalesDashboardDto data)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Spacing(15);

            // KPI Cards Row
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => Card(c, "My Companies", data.TotalCompanies.ToString(), Colors.Blue.Lighten5, Colors.Blue.Darken2));
                row.Spacing(10);
                row.RelativeItem().Element(c => Card(c, "My Orders", data.TotalOrders.ToString(), Colors.Green.Lighten5, Colors.Green.Darken2));
                row.Spacing(10);
                row.RelativeItem().Element(c => Card(c, "My Sales USD", $"${data.TotalSalesUsd:N2}", Colors.Amber.Lighten5, Colors.Amber.Darken3));
            });

            // Top Solutions
            column.Item().Text("Top Solutions").Bold().FontSize(12);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                table.Header(h =>
                {
                    h.Cell().Element(HeaderStyle).Text("Solution");
                    h.Cell().Element(HeaderStyle).Text("Items Confirmed");
                    h.Cell().Element(HeaderStyle).AlignRight().Text("USD Revenue");
                });
                foreach (var x in data.TopSolutions)
                {
                    table.Cell().Element(RowStyle).Text(x.Solution);
                    table.Cell().Element(RowStyle).Text(x.Count.ToString());
                    table.Cell().Element(RowStyle).AlignRight().Text($"${x.UsdAmount:N2}");
                }
            });
        });

        static IContainer HeaderStyle(IContainer container) => container.Background(Colors.Grey.Lighten3).Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Medium);
        static IContainer RowStyle(IContainer container) => container.Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
    }

    private void Card(IContainer container, string title, string val, string bgColor, string textColor)
    {
        container.Background(bgColor).Padding(10).AlignLeft().Column(c =>
        {
            c.Item().Text(title).FontSize(8).FontColor(textColor).SemiBold();
            c.Item().Text(val).FontSize(14).FontColor(textColor).Bold();
        });
    }

    private void ComposeFooter(IContainer container)
    {
         container.AlignCenter().Text(x =>
         {
             x.Span("Page ");
             x.CurrentPageNumber();
             x.Span(" of ");
             x.TotalPages();
         });
    }
}
