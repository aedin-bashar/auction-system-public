using AuctionSystem.API.Extensions;
using AuctionSystem.Application.Payments.Admin.TransactionManagement;
using AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactionDetail;
using AuctionSystem.Application.Payments.Admin.TransactionManagement.GetAdminTransactions;
using AuctionSystem.Application.Payments.Admin.TransactionManagement.ProcessRefund;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionSystem.API.Controllers;

[ApiController]
[Route("api/admin/transactions")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminTransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminTransactionsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminTransactionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminTransactionListItemDto>>> GetTransactions(CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        var transactions = await _mediator.Send(new GetAdminTransactionsQuery(requesterUserId), cancellationToken);
        return Ok(transactions);
    }

    [HttpGet("{transactionId:guid}")]
    [ProducesResponseType(typeof(AdminTransactionDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminTransactionDetailDto>> GetTransaction(Guid transactionId, CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        var transaction = await _mediator.Send(new GetAdminTransactionDetailQuery(requesterUserId, transactionId), cancellationToken);
        return Ok(transaction);
    }

    [HttpPost("{transactionId:guid}/refund")]
    [ProducesResponseType(typeof(AdminTransactionDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminTransactionDetailDto>> RefundTransaction(
        Guid transactionId,
        [FromBody] RefundTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var requesterUserId = User.GetRequiredUserId();
        var transaction = await _mediator.Send(
            new ProcessRefundCommand(requesterUserId, transactionId, request.Reason),
            cancellationToken);

        return Ok(transaction);
    }

    public sealed record RefundTransactionRequest(string? Reason);
}
