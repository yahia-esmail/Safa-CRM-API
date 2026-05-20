using Application.Features.Orders;

namespace Application.Common.Interfaces;

public interface IInvoiceService
{
    byte[] GenerateInvoicePdf(OrderDto order);
}
