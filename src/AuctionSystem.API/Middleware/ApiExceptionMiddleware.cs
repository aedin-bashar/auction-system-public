using System.Net;
using System.Text.Json;
using FluentValidation;

namespace AuctionSystem.API.Middleware;

public sealed class ApiExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request was canceled by the client.");
        }
        catch (Exception ex)
        {
            var (statusCode, error, details) = Map(ex);

            if ((int)statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Unhandled API exception.");
            }
            else
            {
                _logger.LogWarning(
                    "Handled API exception. StatusCode={StatusCode}, Error={Error}, Message={Message}",
                    (int)statusCode,
                    error,
                    ex.Message);
            }

            await WriteResponseAsync(context, statusCode, error, details);
        }
    }

    private static async Task WriteResponseAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string error,
        object? details)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            error,
            details
        }, JsonOptions));
    }

    private static (HttpStatusCode StatusCode, string Error, object? Details) Map(Exception ex)
    {
        return ex switch
        {
            ValidationException validation => (HttpStatusCode.BadRequest, "ValidationFailed",
                validation.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Forbidden", ex.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "NotFound", ex.Message),
            ArgumentException => (HttpStatusCode.BadRequest, "InvalidInput", ex.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, "InvalidOperation", ex.Message),
            _ => (HttpStatusCode.InternalServerError, "ServerError", "An unexpected error occurred.")
        };
    }
}
