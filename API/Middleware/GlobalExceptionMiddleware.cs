using System.Net;
using System.Text.Json;

namespace API.Middleware;

/// <summary>
/// Global exception handler — maps exceptions to correct HTTP status codes for ALL endpoints.
/// This eliminates the need for repetitive try-catch blocks in every controller.
/// </summary>
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            // 400 — Invalid input / business rule violation
            ArgumentException e       => (HttpStatusCode.BadRequest, e.Message),
            InvalidOperationException e => (HttpStatusCode.BadRequest, e.Message),

            // 401 — Authentication required
            UnauthorizedAccessException e => (HttpStatusCode.Unauthorized, e.Message),

            // 403 — Forbidden (access denied)
            // Use a custom type if needed

            // 404 — Not found
            KeyNotFoundException e    => (HttpStatusCode.NotFound, e.Message),

            // 500 — Unexpected
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        var body = JsonSerializer.Serialize(new { message }, _jsonOptions);
        await context.Response.WriteAsync(body);
    }
}
