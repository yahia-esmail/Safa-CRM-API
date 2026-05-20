using Application.Features.AI;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class AiController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Generate a professional email draft for a company using Gemini AI.</summary>
    [HttpPost("compose-email")]
    public async Task<IActionResult> ComposeEmail([FromBody] ComposeEmailRequest request) =>
        Ok(await Mediator.Send(new ComposeEmailCommand(request, CurrentUserId)));

    /// <summary>Get an AI-generated lead score (0–100) with explanation for a company.</summary>
    [HttpGet("lead-score/{companyId:guid}")]
    public async Task<IActionResult> GetLeadScore(Guid companyId) =>
        Ok(await Mediator.Send(new GetLeadScoreQuery(companyId, CurrentUserId)));

    /// <summary>Get the AI-recommended next action for a company.</summary>
    [HttpGet("next-action/{companyId:guid}")]
    public async Task<IActionResult> GetNextAction(Guid companyId) =>
        Ok(await Mediator.Send(new GetNextActionQuery(companyId, CurrentUserId)));

    /// <summary>Get AI-generated sales insights summary for a date range (Admin only).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("insights")]
    public async Task<IActionResult> GetInsights(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to) =>
        Ok(await Mediator.Send(new GetSalesInsightsQuery(from, to)));
}
