using Application.Features.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// SuperAdmin-only endpoints for platform-level management.
/// All routes under /api/superadmin require SuperAdmin role.
/// </summary>
[ApiController]
[Route("api/superadmin")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController(IMediator mediator) : ControllerBase
{
    // ── Dashboard ──────────────────────────────────────────────────────────────

    /// <summary>GET /api/superadmin/dashboard — Platform-wide KPI summary.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await mediator.Send(new GetSuperAdminDashboardQuery());
        return Ok(result);
    }

    // ── Subscription Plans ─────────────────────────────────────────────────────

    /// <summary>GET /api/superadmin/plans — List all subscription plans.</summary>
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
        => Ok(await mediator.Send(new GetPlansQuery()));

    /// <summary>POST /api/superadmin/plans — Create a new subscription plan.</summary>
    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest req)
    {
        try
        {
            var result = await mediator.Send(new CreatePlanCommand(req.Name, req.MaxAdmins, req.MaxSales, req.Price));
            return CreatedAtAction(nameof(GetPlans), result);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PUT /api/superadmin/plans/{id} — Update an existing plan.</summary>
    [HttpPut("plans/{id:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] CreatePlanRequest req)
    {
        try
        {
            var result = await mediator.Send(new UpdatePlanCommand(id, req.Name, req.MaxAdmins, req.MaxSales, req.Price));
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>DELETE /api/superadmin/plans/{id} — Delete a plan (only if no tenants use it).</summary>
    [HttpDelete("plans/{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        try
        {
            await mediator.Send(new DeletePlanCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ── Tenants ────────────────────────────────────────────────────────────────

    /// <summary>GET /api/superadmin/tenants — List all tenants with summary stats.</summary>
    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants()
        => Ok(await mediator.Send(new GetTenantsQuery()));

    /// <summary>GET /api/superadmin/tenants/{id} — Get a single tenant with full detail stats.</summary>
    [HttpGet("tenants/{id:guid}")]
    public async Task<IActionResult> GetTenantById(Guid id)
    {
        try { return Ok(await mediator.Send(new GetTenantByIdQuery(id))); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    /// <summary>POST /api/superadmin/tenants — Create a new tenant (client company onboarding).</summary>
    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest req)
    {
        try
        {
            var result = await mediator.Send(new CreateTenantCommand(req.Name, req.Industry, req.SubscriptionPlanId, req.MonthsDuration));
            return CreatedAtAction(nameof(GetTenantById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PUT /api/superadmin/tenants/{id} — Update tenant info, change plan, or extend subscription.</summary>
    [HttpPut("tenants/{id:guid}")]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest req)
    {
        try
        {
            var result = await mediator.Send(new UpdateTenantCommand(id, req.Name, req.Industry, req.SubscriptionPlanId, req.SubscriptionEnd));
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PATCH /api/superadmin/tenants/{id}/toggle — Activate or deactivate a tenant (and all its users).</summary>
    [HttpPatch("tenants/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleTenant(Guid id, [FromBody] ToggleStatusRequest req)
    {
        try
        {
            await mediator.Send(new ToggleTenantStatusCommand(id, req.IsActive));
            return Ok(new { message = req.IsActive ? "Tenant activated." : "Tenant deactivated. All users suspended." });
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // ── Audit Logs ─────────────────────────────────────────────────────────────

    /// <summary>GET /api/superadmin/audit-logs — Paginated audit trail with optional filters.</summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] string? action,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await mediator.Send(new GetAuditLogsQuery(entityName, action, page, pageSize));
        return Ok(result);
    }
}

// ── Request DTOs ───────────────────────────────────────────────────────────────
public record CreatePlanRequest(string Name, int MaxAdmins, int MaxSales, decimal Price);
public record CreateTenantRequest(string Name, string Industry, Guid SubscriptionPlanId, int MonthsDuration);
public record UpdateTenantRequest(string Name, string Industry, Guid SubscriptionPlanId, DateTime SubscriptionEnd);
public record ToggleStatusRequest(bool IsActive);
