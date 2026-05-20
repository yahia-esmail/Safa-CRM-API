using Application.Common.Interfaces;
using Application.Features.Orders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class InvoicesController(IMediator mediator, IInvoiceService invoiceService) : BaseController(mediator)
{
    [HttpGet("{orderId:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid orderId)
    {
        try
        {
            var order = await Mediator.Send(new GetOrderByIdQuery(orderId, CurrentUserId, IsAdmin));
            var pdfBytes = invoiceService.GenerateInvoicePdf(order);
            
            return File(pdfBytes, "application/pdf", $"Invoice_{order.InvoiceNumber.Replace("/", "_")}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Order/Invoice not found." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
