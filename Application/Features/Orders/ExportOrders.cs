using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Orders;

public record GetOrdersExportQuery(
    DateTime? From,
    DateTime? To,
    string? Status,
    Guid? CompanyId,
    Guid CurrentUserId,
    bool IsAdmin) : IRequest<byte[]>;

public class GetOrdersExportHandler(IAppDbContext context, IExportService exportService)
    : IRequestHandler<GetOrdersExportQuery, byte[]>
{
    public async Task<byte[]> Handle(GetOrdersExportQuery q, CancellationToken ct)
    {
        var query = context.SalesOrders
            .Include(o => o.Company)
            .Include(o => o.CreatedBy)
            .AsQueryable();

        if (!q.IsAdmin)
            query = query.Where(o => o.CreatedByUserId == q.CurrentUserId);

        if (q.From.HasValue)
            query = query.Where(o => o.CreatedAt >= q.From.Value);

        if (q.To.HasValue)
            query = query.Where(o => o.CreatedAt <= q.To.Value);

        if (q.CompanyId.HasValue)
            query = query.Where(o => o.CompanyId == q.CompanyId.Value);

        if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<OrderStatus>(q.Status, true, out var parsedStatus))
            query = query.Where(o => o.Status == parsedStatus);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);

        var exportRows = orders.Select(o => new OrderExportRow(
            o.InvoiceNumber,
            o.Company.EnglishName,
            o.SaleOrderType,
            o.Status.ToString(),
            o.OriginalCurrency.ToString(),
            o.OriginalAmount,
            o.UsdAmount,
            o.CreatedBy.Name,
            o.CreatedAt
        ));

        return exportService.ExportOrders(exportRows);
    }
}
