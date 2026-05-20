using Application.Features.Companies.Import;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class CompanyImportController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Download the official Excel import template.</summary>
    [HttpGet("template")]
    public IActionResult GetTemplate()
    {
        // File is stored in wwwroot/templates/
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates",
                                "Safa_CRM_Company_Import_Template.xlsx");
        if (!System.IO.File.Exists(path))
            return NotFound(new { message = "Template file not found on server." });

        var bytes = System.IO.File.ReadAllBytes(path);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Safa_CRM_Company_Import_Template.xlsx");
    }

    /// <summary>Upload Excel file, run validation, return preview (no DB write).</summary>
    [HttpPost("preview")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max
    public async Task<IActionResult> Preview(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Please upload a valid .xlsx file." });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only .xlsx files are accepted." });

        await using var stream = file.OpenReadStream();
        var result = await Mediator.Send(new PreviewImportCommand(stream, file.FileName, CurrentUserId));
        return Ok(result);
    }

    /// <summary>Confirm a previously previewed import and save valid rows to the database.</summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmImportRequest request)
    {
        var result = await Mediator.Send(new ConfirmImportCommand(request.ImportId, CurrentUserId));
        return Ok(result);
    }

    /// <summary>View import history logs (Admin only).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs() =>
        Ok(await Mediator.Send(new GetImportLogsQuery()));
}

public record ConfirmImportRequest(string ImportId);
