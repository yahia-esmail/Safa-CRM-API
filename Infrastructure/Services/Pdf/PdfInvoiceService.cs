using Application.Common.Interfaces;
using Application.Features.Orders;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.Services.Pdf;

public class PdfInvoiceService : IInvoiceService
{
    public PdfInvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(OrderDto order)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(c => ComposeHeader(c, order));
                page.Content().Element(c => ComposeContent(c, order));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, OrderDto order)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("INVOICE").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Invoice #: {order.InvoiceNumber}");
                column.Item().Text($"Issue Date: {order.CreatedAt:dd MMM yyyy}");
                
                if (!string.IsNullOrEmpty(order.OrderReference))
                    column.Item().Text($"Reference: {order.OrderReference}");
            });

            row.ConstantItem(200).AlignRight().Column(column =>
            {
                column.Item().Text("SAFA CRM").FontSize(14).Bold();
                column.Item().Text("Safa Programming Company");
                column.Item().Text("Email: info@safa-crm.local");
                column.Item().Text("Sales Rep: " + order.CreatedByName);
            });
        });
    }

    private void ComposeContent(IContainer container, OrderDto order)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(20);

            // Bill To
            column.Item().Column(billTo =>
            {
                billTo.Item().Text("Billed To:").SemiBold();
                billTo.Item().Text(order.CompanyName).Bold().FontSize(12);
            });

            // Table
            column.Item().Element(c => ComposeTable(c, order));

            // Totals
            column.Item().AlignRight().Column(totals => 
            {
                totals.Item().Text($"Subtotal ({order.OriginalCurrency}): {order.OriginalAmount:N2}");
                totals.Item().Text($"Total USD: ${order.UsdAmount:N2}").Bold().FontSize(14);
            });
        });
    }

    private void ComposeTable(IContainer container, OrderDto order)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);
                columns.RelativeColumn(3);
                columns.ConstantColumn(120);   // period
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("#");
                header.Cell().Element(CellStyle).Text("Solution / Description");
                header.Cell().Element(CellStyle).Text("Period");
                header.Cell().Element(CellStyle).AlignRight().Text($"Amount ({order.OriginalCurrency})");

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                }
            });

            int i = 1;
            foreach (var item in order.Items)
            {
                var isDiscount = item.Price < 0;

                table.Cell().Element(CellStyle).Text(i++.ToString());

                var desc = item.SolutionName;
                if (!string.IsNullOrEmpty(item.Note)) desc += $"\n{item.Note}";
                if (isDiscount) desc += "\n(Discount)";
                table.Cell().Element(CellStyle).Text(desc);

                var period = (item.StartDate.HasValue && item.EndDate.HasValue)
                    ? $"{item.StartDate:dd/MM/yyyy}\n{item.EndDate:dd/MM/yyyy}"
                    : "-";
                table.Cell().Element(CellStyle).Text(period);

                table.Cell().Element(CellStyle).AlignRight()
                    .Text($"{item.Price:N2}")
                    .FontColor(isDiscount ? Colors.Red.Medium : Colors.Black);

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            }
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
