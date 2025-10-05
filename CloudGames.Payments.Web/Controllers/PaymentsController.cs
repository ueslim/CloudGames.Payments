using System.Security.Claims;
using CloudGames.Payments.Application.Commands;
using CloudGames.Payments.Application.DTOs;
using CloudGames.Payments.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudGames.Payments.Web.Controllers;

[ApiController]
[Route("/api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentRequestDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var resp = await _mediator.Send(new InitiatePaymentCommand(userId, dto));
        return Accepted($"/api/payments/{resp.PaymentId}/status", resp);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Status(Guid id)
    {
        var resp = await _mediator.Send(new GetPaymentStatusQuery(id));
        return Ok(resp);
    }
}
