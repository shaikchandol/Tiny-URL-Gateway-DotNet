using System.Text.Json;
using FluentValidation;

namespace TinyUrl.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await WriteError(context, StatusCodes.Status404NotFound, "NOT_FOUND", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            var status = ex.Message.Contains("expired") ? StatusCodes.Status410Gone : StatusCodes.Status409Conflict;
            await WriteError(context, status, "CONFLICT", ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            var errors = ex.Errors.Select(e => e.ErrorMessage);
            await WriteError(context, StatusCodes.Status400BadRequest, "VALIDATION_ERROR", string.Join("; ", errors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteError(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteError(HttpContext context, int status, string error, string message)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new { error, message });
        await context.Response.WriteAsync(body);
    }
}
