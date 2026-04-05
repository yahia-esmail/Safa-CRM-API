using Application.Features.Rates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class RatesController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet("today")]
    public async Task<IActionResult> Today()
    {
        try { return Ok(await Mediator.Send(new GetTodayRateQuery())); }
        catch (KeyNotFoundException) { return NotFound(new { message = "No rate data found." }); }
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] DateTime from, [FromQuery] DateTime to) =>
        Ok(await Mediator.Send(new GetRateHistoryQuery(from, to)));

    [Authorize(Roles = "Admin")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        await Mediator.Send(new RefreshRatesCommand());
        return Ok(new { message = "Exchange rates refreshed successfully." });
    }
}
