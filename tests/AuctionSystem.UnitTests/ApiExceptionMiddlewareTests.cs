using AuctionSystem.API.Middleware;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuctionSystem.UnitTests;

public class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task Invoke_WhenRequestIsAborted_DoesNotWriteErrorResponse()
    {
        var result = await InvokeAsync(new TaskCanceledException(), abortRequest: true);

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(string.Empty, result.Body);
    }

    [Fact]
    public async Task Invoke_WhenArgumentExceptionIsThrown_ReturnsBadRequestPayload()
    {
        var result = await InvokeAsync(new ArgumentException("Bad input."));

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("application/json", result.ContentType);
        Assert.Contains("InvalidInput", result.Body);
        Assert.Contains("Bad input.", result.Body);
    }

    [Fact]
    public async Task Invoke_WhenValidationExceptionIsThrown_ReturnsValidationDetails()
    {
        var exception = new ValidationException(
        [
            new ValidationFailure("Email", "Email is required."),
            new ValidationFailure("Password", "Password is required.")
        ]);

        var result = await InvokeAsync(exception);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains("ValidationFailed", result.Body);
        Assert.Contains("Email", result.Body);
        Assert.Contains("Password is required.", result.Body);
    }

    [Fact]
    public async Task Invoke_WhenUnauthorizedAccessExceptionIsThrown_ReturnsForbiddenPayload()
    {
        var result = await InvokeAsync(new UnauthorizedAccessException("Access denied."));

        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Contains("Forbidden", result.Body);
        Assert.Contains("Access denied.", result.Body);
    }

    [Fact]
    public async Task Invoke_WhenKeyNotFoundExceptionIsThrown_ReturnsNotFoundPayload()
    {
        var result = await InvokeAsync(new KeyNotFoundException("Auction was not found."));

        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Contains("NotFound", result.Body);
        Assert.Contains("Auction was not found.", result.Body);
    }

    [Fact]
    public async Task Invoke_WhenInvalidOperationExceptionIsThrown_ReturnsBadRequestPayload()
    {
        var result = await InvokeAsync(new InvalidOperationException("Auction is already ended."));

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains("InvalidOperation", result.Body);
        Assert.Contains("Auction is already ended.", result.Body);
    }

    [Fact]
    public async Task Invoke_WhenOperationIsCanceledWithoutRequestAbort_ReturnsServerErrorPayload()
    {
        var result = await InvokeAsync(new OperationCanceledException("Timed out."));

        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Contains("ServerError", result.Body);
        Assert.Contains("An unexpected error occurred.", result.Body);
        Assert.DoesNotContain("Timed out.", result.Body);
    }

    [Fact]
    public async Task Invoke_WhenUnhandledExceptionIsThrown_ReturnsSanitizedServerErrorPayload()
    {
        var result = await InvokeAsync(new Exception("Sensitive details."));

        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Contains("ServerError", result.Body);
        Assert.Contains("An unexpected error occurred.", result.Body);
        Assert.DoesNotContain("Sensitive details.", result.Body);
    }

    private static async Task<(int StatusCode, string? ContentType, string Body)> InvokeAsync(Exception exception, bool abortRequest = false)
    {
        var context = new DefaultHttpContext();
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        if (abortRequest)
        {
            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();
            context.RequestAborted = cancellationSource.Token;
        }

        var middleware = new ApiExceptionMiddleware(
            _ => throw exception,
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.Invoke(context);

        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody);
        var body = await reader.ReadToEndAsync();

        return (context.Response.StatusCode, context.Response.ContentType, body);
    }
}