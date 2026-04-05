using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Features.Orders;

// --- DTOs ---
public record OrderItemDto(
    Guid Id, Guid SolutionId, string SolutionName,
    decimal Price, string Currency, string? Note);

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
    decimal OriginalAmount,
    decimal UsdRateAtTime,
    decimal UsdAmount,
    string? Attachment,
    DateTime CreatedAt,
    IEnumerable<OrderItemDto> Items);

public record OrderItemRequest(Guid SolutionId, decimal Price, string Currency, string? Note);

public record CreateOrderRequest(
    Guid CompanyId,
    string? OrderReference,
    string SaleOrderType,
    string? PaymentMethod,
    string OriginalCurrency,
    decimal OriginalAmount,
    string? Attachment,
    IEnumerable<OrderItemRequest> Items);

public record UpdateOrderRequest(
    string? OrderReference,
    string SaleOrderType,
    string Status,
    string? PaymentMethod,
    string? Attachment);

// --- Commands & Queries ---
public record GetOrdersQuery(Guid CurrentUserId, bool IsAdmin) : IRequest<IEnumerable<OrderDto>>;
public record GetOrderByIdQuery(Guid Id, Guid CurrentUserId, bool IsAdmin) : IRequest<OrderDto>;
public record GetCompanyOrdersQuery(Guid CompanyId, Guid CurrentUserId, bool IsAdmin) : IRequest<IEnumerable<OrderDto>>;
public record CreateOrderCommand(CreateOrderRequest Request, Guid CurrentUserId) : IRequest<OrderDto>;
public record UpdateOrderCommand(Guid Id, UpdateOrderRequest Request, Guid CurrentUserId, bool IsAdmin) : IRequest<OrderDto>;
public record DeleteOrderCommand(Guid Id, Guid CurrentUserId, bool IsAdmin) : IRequest;

// --- Handlers ---
public class GetOrdersHandler(IAppDbContext context) : IRequestHandler<GetOrdersQuery, IEnumerable<OrderDto>>
{
    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersQuery q, CancellationToken ct)
    {
        var query = context.SalesOrders
            .Include(o => o.Company)
            .Include(o => o.CreatedBy)
            .Include(o => o.Items).ThenInclude(i => i.Solution)
            .AsQueryable();

        if (!q.IsAdmin)
            query = query.Where(o => o.CreatedByUserId == q.CurrentUserId);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
        return orders.Select(o => o.ToOrderDto());
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

        if (!Enum.TryParse<Currency>(r.OriginalCurrency, true, out var currency))
            throw new ArgumentException($"Invalid currency: {r.OriginalCurrency}");

        var rate = await rateRepo.GetTodayRateAsync() ?? await rateRepo.GetLatestRateAsync()
            ?? throw new InvalidOperationException("No exchange rate available. Please refresh rates first.");

        var usdRate = IExchangeRateRepository.GetRateForCurrency(rate, currency);
        var usdAmount = IExchangeRateRepository.ConvertToUsd(rate, currency, r.OriginalAmount);
        var invoiceNumber = await orderRepo.GenerateInvoiceNumberAsync(DateTime.UtcNow.Year);

        var order = new SalesOrder
        {
            InvoiceNumber = invoiceNumber,
            CompanyId = r.CompanyId,
            CreatedByUserId = cmd.CurrentUserId,
            OrderReference = r.OrderReference,
            SaleOrderType = r.SaleOrderType,
            Status = OrderStatus.Draft,
            PaymentMethod = r.PaymentMethod,
            OriginalCurrency = currency,
            OriginalAmount = r.OriginalAmount,
            UsdRateAtTime = usdRate,
            UsdAmount = usdAmount,
            Attachment = r.Attachment
        };

        foreach (var item in r.Items)
        {
            if (!Enum.TryParse<Currency>(item.Currency, true, out var itemCurrency))
                throw new ArgumentException($"Invalid currency: {item.Currency}");

            order.Items.Add(new SalesOrderItem
            {
                SolutionId = item.SolutionId,
                Price = item.Price,
                Currency = itemCurrency,
                Note = item.Note
            });
        }

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

public class UpdateOrderHandler(IAppDbContext context) : IRequestHandler<UpdateOrderCommand, OrderDto>
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

        order.OrderReference = cmd.Request.OrderReference;
        order.SaleOrderType = cmd.Request.SaleOrderType;
        order.Status = status;
        order.PaymentMethod = cmd.Request.PaymentMethod;
        order.Attachment = cmd.Request.Attachment;
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
            i.Price, i.Currency.ToString(), i.Note)));
}
