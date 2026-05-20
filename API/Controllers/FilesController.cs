using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/files")]
public class FilesController(IFileUploadService fileUploadService) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(11 * 1024 * 1024)] // Allow slight overhead above 10MB
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
    {
        try
        {
            var url = await fileUploadService.UploadFileAsync(file, "uploads");
            return Ok(new { Url = url });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while uploading the file." });
        }
    }
}
