using Application.Common;
using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Features.Orders;

// --- DTOs ---
public record OrderItemDto(
    Guid Id,
    Guid SolutionId,
    string SolutionName,
    decimal Price,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Note);

public record OrderDto(
    Guid Id,
    string InvoiceNumber,
    Guid CompanyId,
    string CompanyName,
    Guid CreatedByUserId,
    string CreatedByName,
    string? OrderReference,
    string SaleOrderType,
    string Status,
    string? PaymentMethod,
    string OriginalCurrency,
    decimal OriginalAmount,      // Calculated: sum of all item prices
    decimal UsdRateAtTime,
    decimal UsdAmount,
    string? Attachment,
    DateTime CreatedAt,
    IEnumerable<OrderItemDto> Items);

/// <summary>
/// Per-item request. Currency is inherited from the order level.
/// StartDate / EndDate are optional — used for subscriptions.
/// Price can be negative to represent a discount line.
/// </summary>
public record OrderItemRequest(
    Guid SolutionId,
    decimal Price,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Note);

/// <summary>
/// OriginalAmount is NOT sent by the client — it is calculated server-side
/// as the sum of all item prices (including negative/discount lines).
/// OriginalCurrency applies to ALL items in the order.
/// </summary>
public record CreateOrderRequest(
    Guid CompanyId,
    string? OrderReference,
    string SaleOrderType,
    string? PaymentMethod,
    string OriginalCurrency,
    string? Attachment,
    IEnumerable<OrderItemRequest> Items);

public record UpdateOrderRequest(
    string? OrderReference,
    string SaleOrderType,
    string Status,
    string? PaymentMethod,
    string? Attachment,
    string? CancellationReason = null);


// --- Commands & Queries ---
public record GetOrdersQuery(Guid CurrentUserId, bool IsAdmin, int Page = 1, int Size = 20) : IRequest<PagedResult<OrderDto>>;
public record GetOrderByIdQuery(Guid Id, Guid CurrentUserId, bool IsAdmin) : IRequest<OrderDto>;
public record GetCompanyOrdersQuery(Guid CompanyId, Guid CurrentUserId, bool IsAdmin) : IRequest<IEnumerable<OrderDto>>;
public record CreateOrderCommand(CreateOrderRequest Request, Guid CurrentUserId) : IRequest<OrderDto>;
public record UpdateOrderCommand(Guid Id, UpdateOrderRequest Request, Guid CurrentUserId, bool IsAdmin) : IRequest<OrderDto>;
public record DeleteOrderCommand(Guid Id, Guid CurrentUserId, bool IsAdmin) : IRequest;

// --- Handlers ---
public class GetOrdersHandler(IAppDbContext context) : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery q, CancellationToken ct)
    {
        var query = context.SalesOrders
            .Include(o => o.Company)
            .Include(o => o.CreatedBy)
            .Include(o => o.Items).ThenInclude(i => i.Solution)
            .AsQueryable();

        if (!q.IsAdmin)
            query = query.Where(o => o.CreatedByUserId == q.CurrentUserId);

        var total = await query.CountAsync(ct);
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((q.Page - 1) * q.Size)
            .Take(q.Size)
            .ToListAsync(ct);

        return new PagedResult<OrderDto>(orders.Select(o => o.ToOrderDto()), total, q.Page, q.Size);
    }
}

public class GetOrderByIdHandler(IAppDbContext context) : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery q, CancellationToken ct)
    {
        var order = await context.SalesOrders
            .Include(o => o.Company).Include(o => o.CreatedBy)
            .Include(o => o.Items).ThenInclude(i => i.Solution)
            .FirstOrDefaultAsync(o => o.Id == q.Id, ct)
            ?? throw new KeyNotFoundException("Order not found.");

        if (!q.IsAdmin && order.CreatedByUserId != q.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        return order.ToOrderDto();
    }
}

public class GetCompanyOrdersHandler(IAppDbContext context) : IRequestHandler<GetCompanyOrdersQuery, IEnumerable<OrderDto>>
{
    public async Task<IEnumerable<OrderDto>> Handle(GetCompanyOrdersQuery q, CancellationToken ct)
    {
        var company = await context.Companies.FindAsync([q.CompanyId], ct)
            ?? throw new KeyNotFoundException("Company not found.");
        if (!q.IsAdmin && company.AssignedToUserId != q.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        var orders = await context.SalesOrders
            .Include(o => o.Company).Include(o => o.CreatedBy)
            .Include(o => o.Items).ThenInclude(i => i.Solution)
            .Where(o => o.CompanyId == q.CompanyId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return orders.Select(o => o.ToOrderDto());
    }
}

public class CreateOrderHandler(
    IAppDbContext context,
    ISalesOrderRepository orderRepo,
    IExchangeRateRepository rateRepo)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var r = cmd.Request;

        if (!r.Items.Any())
            throw new ArgumentException("An order must have at least one item.");

        if (!Enum.TryParse<Currency>(r.OriginalCurrency, true, out var currency))
            throw new ArgumentException($"Invalid currency: {r.OriginalCurrency}");

        // OriginalAmount = sum of ALL item prices (negative = discount)
        var originalAmount = r.Items.Sum(i => i.Price);

        var rate = await rateRepo.GetTodayRateAsync() ?? await rateRepo.GetLatestRateAsync()
            ?? throw new InvalidOperationException("No exchange rate available. Please refresh rates first.");

        var usdRate   = IExchangeRateRepository.GetRateForCurrency(rate, currency);
        var usdAmount = IExchangeRateRepository.ConvertToUsd(rate, currency, originalAmount);
        var invoiceNumber = await orderRepo.GenerateInvoiceNumberAsync(DateTime.UtcNow.Year);

        var order = new SalesOrder
        {
            InvoiceNumber    = invoiceNumber,
            CompanyId        = r.CompanyId,
            CreatedByUserId  = cmd.CurrentUserId,
            OrderReference   = r.OrderReference,
            SaleOrderType    = r.SaleOrderType,
            Status           = OrderStatus.Draft,
            PaymentMethod    = r.PaymentMethod,
            OriginalCurrency = currency,
            OriginalAmount   = originalAmount,   // auto-calculated
            UsdRateAtTime    = usdRate,
            UsdAmount        = usdAmount,
            Attachment       = r.Attachment
        };

        foreach (var item in r.Items)
        {
            // Validate date range if provided
            if (item.StartDate.HasValue && item.EndDate.HasValue && item.EndDate < item.StartDate)
                throw new ArgumentException($"EndDate cannot be before StartDate for solution {item.SolutionId}.");

            order.Items.Add(new SalesOrderItem
            {
                SolutionId = item.SolutionId,
                Price      = item.Price,          // negative = discount line
                StartDate  = item.StartDate,
                EndDate    = item.EndDate,
                Note       = item.Note
            });
        }

        // Auto-generate linked invoice
        order.Invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            IssueDate     = DateTime.UtcNow
        };

        await orderRepo.AddAsync(order);
        await orderRepo.SaveChangesAsync();

        // Reload with includes
        var full = await context.SalesOrders
            .Include(o => o.Company).Include(o => o.CreatedBy)
            .Include(o => o.Items).ThenInclude(i => i.Solution)
            .FirstAsync(o => o.Id == order.Id, ct);

        return full.ToOrderDto();
    }
}

public class UpdateOrderHandler(IAppDbContext context, INotificationService notificationService) : IRequestHandler<UpdateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(UpdateOrderCommand cmd, CancellationToken ct)
    {
        var order = await context.SalesOrders
            .Include(o => o.Company).Include(o => o.CreatedBy)
            .Include(o => o.Items).ThenInclude(i => i.Solution)
            .FirstOrDefaultAsync(o => o.Id == cmd.Id, ct)
            ?? throw new KeyNotFoundException("Order not found.");

        if (!cmd.IsAdmin && order.CreatedByUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");

        if (!Enum.TryParse<OrderStatus>(cmd.Request.Status, true, out var status))
            throw new ArgumentException($"Invalid status: {cmd.Request.Status}");

        var oldStatus = order.Status;

        order.OrderReference = cmd.Request.OrderReference;
        order.SaleOrderType  = cmd.Request.SaleOrderType;
        order.Status         = status;
        order.PaymentMethod  = cmd.Request.PaymentMethod;
        order.Attachment     = cmd.Request.Attachment;

        // Process status changes
        if (oldStatus != status)
        {
            if (status == OrderStatus.Confirmed)
            {
                order.ConfirmedAt = DateTime.UtcNow;
                
                // Notify Creator + Admins
                var recipients = await context.Users
                    .Where(u => u.TenantId == order.TenantId && u.Role == Role.Admin && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync(ct);

                if (!recipients.Contains(order.CreatedByUserId))
                {
                    recipients.Add(order.CreatedByUserId);
                }

                foreach (var rId in recipients)
                {
                    await notificationService.SendAsync(
                        rId,
                        "Sales Order Confirmed",
                        $"Sales order {order.InvoiceNumber} for company '{order.Company.EnglishName}' has been confirmed.",
                        NotificationType.OrderConfirmed,
                        "SalesOrder",
                        order.Id.ToString());
                }
            }
            else if (status == OrderStatus.Cancelled)
            {
                order.CancelledAt = DateTime.UtcNow;
                order.CancellationReason = cmd.Request.CancellationReason;

                // Notify Creator + Admins
                var recipients = await context.Users
                    .Where(u => u.TenantId == order.TenantId && u.Role == Role.Admin && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync(ct);

                if (!recipients.Contains(order.CreatedByUserId))
                {
                    recipients.Add(order.CreatedByUserId);
                }

                var reasonText = !string.IsNullOrWhiteSpace(cmd.Request.CancellationReason) 
                    ? $" Reason: {cmd.Request.CancellationReason}" 
                    : "";

                foreach (var rId in recipients)
                {
                    await notificationService.SendAsync(
                        rId,
                        "Sales Order Cancelled",
                        $"Sales order {order.InvoiceNumber} for company '{order.Company.EnglishName}' has been cancelled.{reasonText}",
                        NotificationType.OrderCancelled,
                        "SalesOrder",
                        order.Id.ToString());
                }
            }
        }

        await context.SaveChangesAsync(ct);
        return order.ToOrderDto();
    }
}

public class DeleteOrderHandler(IAppDbContext context) : IRequestHandler<DeleteOrderCommand>
{
    public async Task Handle(DeleteOrderCommand cmd, CancellationToken ct)
    {
        var order = await context.SalesOrders.FindAsync([cmd.Id], ct)
            ?? throw new KeyNotFoundException("Order not found.");
        if (!cmd.IsAdmin && order.CreatedByUserId != cmd.CurrentUserId)
            throw new UnauthorizedAccessException("Access denied.");
        if (order.Status == OrderStatus.Confirmed)
            throw new InvalidOperationException("Cannot delete a confirmed order.");
        context.SalesOrders.Remove(order);
        await context.SaveChangesAsync(ct);
    }
}

// Extension mapper
internal static class OrderExtensions
{
    internal static OrderDto ToOrderDto(this SalesOrder o) => new(
        o.Id, o.InvoiceNumber, o.CompanyId, o.Company?.EnglishName ?? "",
        o.CreatedByUserId, o.CreatedBy?.Name ?? "",
        o.OrderReference, o.SaleOrderType, o.Status.ToString(),
        o.PaymentMethod, o.OriginalCurrency.ToString(), o.OriginalAmount,
        o.UsdRateAtTime, o.UsdAmount, o.Attachment, o.CreatedAt,
        o.Items.Select(i => new OrderItemDto(
            i.Id, i.SolutionId, i.Solution?.Name ?? "",
            i.Price, i.StartDate, i.EndDate, i.Note)));
}
