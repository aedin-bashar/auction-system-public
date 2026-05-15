using AuctionSystem.Application.Users.PaymentMethods;
using AuctionSystem.Application.Users.PaymentMethods.AddPaymentMethod;
using AuctionSystem.Application.Users.PaymentMethods.GetPaymentMethods;
using AuctionSystem.Application.Users.PaymentMethods.RemovePaymentMethod;
using AuctionSystem.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/payment")]
[Authorize]
public sealed class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentMethodDto>> Add(
        [FromBody] AddPaymentMethodRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var result = await _mediator.Send(
            new AddPaymentMethodCommand(
                userId,
                request.Type,
                request.Provider,
                request.Last4,
                request.ExpiryMonth,
                request.ExpiryYear,
                request.HolderName,
                request.IsDefault),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentMethodDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PaymentMethodDto>>> Get(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var result = await _mediator.Send(new GetPaymentMethodsQuery(userId), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{paymentMethodId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid paymentMethodId, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        await _mediator.Send(new RemovePaymentMethodCommand(userId, paymentMethodId), cancellationToken);
        return NoContent();
    }

    public sealed record AddPaymentMethodRequest(
        string Type,
        string Provider,
        string Last4,
        int ExpiryMonth,
        int ExpiryYear,
        string? HolderName,
        bool IsDefault);
}
