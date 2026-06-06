using AuctionSystem.Application.Authentication.ForgotPassword;
using AuctionSystem.Application.Authentication.Login;
using AuctionSystem.Application.Authentication.Models;
using AuctionSystem.Application.Authentication.Register;
using AuctionSystem.Application.Authentication.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public AuthController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResultDto>> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] ForgotPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            // Always 200 – never reveal whether the email exists.
            return Ok();
        }

        var resetBaseUrl = BuildResetBaseUrl(Request, _configuration);

        await handler.HandleAsync(
            new ForgotPasswordCommand(request.Email.Trim(), resetBaseUrl),
            cancellationToken);

        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        [FromServices] ResetPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        var validationErrors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            validationErrors[nameof(ResetPasswordRequest.Token)] = ["Reset token is required."];
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            validationErrors[nameof(ResetPasswordRequest.NewPassword)] = ["New password is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ConfirmNewPassword))
        {
            validationErrors[nameof(ResetPasswordRequest.ConfirmNewPassword)] = ["Confirm password is required."];
        }
        else if (request.NewPassword != request.ConfirmNewPassword)
        {
            validationErrors[nameof(ResetPasswordRequest.ConfirmNewPassword)] = ["Passwords must match."];
        }

        if (validationErrors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(validationErrors));
        }

        var result = await handler.HandleAsync(
            new ResetPasswordCommand(request.Token, request.NewPassword),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok();
    }

    private static string BuildResetBaseUrl(HttpRequest request, IConfiguration configuration)
    {
        var configured = configuration["Frontend:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        var origin = $"{request.Scheme}://{request.Host.Value}";

        if (request.PathBase.HasValue)
        {
            return $"{origin}{request.PathBase.Value}";
        }

        return TryGetBasePathFromReferer(request, out var refererBasePath)
            ? $"{origin}{refererBasePath}"
            : origin;
    }

    private static bool TryGetBasePathFromReferer(HttpRequest request, out string basePath)
    {
        basePath = string.Empty;

        var referer = request.Headers.Referer.ToString();
        if (!Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
        {
            return false;
        }

        var refererHost = refererUri.IsDefaultPort
            ? refererUri.Host
            : $"{refererUri.Host}:{refererUri.Port}";

        if (!string.Equals(refererHost, request.Host.Value, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        const string forgotPasswordRoute = "/forgot-password";
        var refererPath = refererUri.AbsolutePath.TrimEnd('/');

        if (!refererPath.EndsWith(forgotPasswordRoute, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        basePath = refererPath[..^forgotPasswordRoute.Length].TrimEnd('/');
        return !string.IsNullOrWhiteSpace(basePath);
    }
}

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword, string ConfirmNewPassword);

