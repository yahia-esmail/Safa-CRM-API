using Application.Features.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class DashboardController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (IsAdmin)
        {
            var result = await Mediator.Send(new GetAdminDashboardQuery(from, to));
            return Ok(result);
        }
        else
        {
            var result = await Mediator.Send(new GetSalesDashboardQuery(CurrentUserId, from, to));
            return Ok(result);
        }
    }
}
