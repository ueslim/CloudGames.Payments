using System.Security.Claims;
using CloudGames.Payments.Application.Commands;
using CloudGames.Payments.Application.DTOs;
using CloudGames.Payments.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CloudGames.Payments.Web.Controllers;

[ApiController]
[Route("/api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentRequestDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        _logger.LogInformation("Initiating payment. UserId={UserId}, GameId={GameId}, Amount={Amount}", userId, dto.GameId, dto.Amount);
        var resp = await _mediator.Send(new InitiatePaymentCommand(userId, dto));
        _logger.LogInformation("Payment created with id {PaymentId} and status {Status}.", resp.PaymentId, resp.Status);
        return Accepted($"/api/payments/{resp.PaymentId}/status", new { paymentId = resp.PaymentId });
    }

    [Authorize]
    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id)
    {
        _logger.LogInformation("Getting payment status for {PaymentId}.", id);
        var resp = await _mediator.Send(new GetPaymentStatusQuery(id));
        _logger.LogInformation("Payment {PaymentId} status is {Status}.", id, resp.Status);
        return Ok(new { status = resp.Status });
    }
}
