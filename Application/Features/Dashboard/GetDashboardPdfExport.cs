using Application.Common.Interfaces;
using Application.Features.Dashboard;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Dashboard;

public record GetDashboardPdfExportQuery(
    DateTime? From,
    DateTime? To,
    Guid CurrentUserId,
    bool IsAdmin) : IRequest<byte[]>;

public class GetDashboardPdfExportHandler(IMediator mediator, IAppDbContext context, IExportService exportService)
    : IRequestHandler<GetDashboardPdfExportQuery, byte[]>
{
    public async Task<byte[]> Handle(GetDashboardPdfExportQuery q, CancellationToken ct)
    {
        var from = q.From ?? DateTime.UtcNow.AddMonths(-12);
        var to = q.To ?? DateTime.UtcNow;

        if (q.IsAdmin)
        {
            var adminData = await mediator.Send(new GetAdminDashboardQuery(q.From, q.To), ct);
            return exportService.GenerateDashboardPdf(adminData, from, to);
        }
        else
        {
            var salesData = await mediator.Send(new GetSalesDashboardQuery(q.CurrentUserId, q.From, q.To), ct);
            var user = await context.Users.FindAsync([q.CurrentUserId], ct)
                ?? throw new KeyNotFoundException("User not found.");

            return exportService.GenerateDashboardPdf(salesData, user.Name, from, to);
        }
    }
}
