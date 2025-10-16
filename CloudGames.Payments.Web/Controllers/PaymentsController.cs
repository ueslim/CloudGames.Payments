using CloudGames.Payments.Application.Commands;
using CloudGames.Payments.Application.DTOs;
using CloudGames.Payments.Application.Queries;
using MediatR;
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

    /// <summary>
    /// Inicia um novo pagamento
    /// </summary>
    /// <remarks>
    /// Em produção, APIM valida o usuário e passa informações via headers.
    /// Em desenvolvimento, userId pode ser passado via header para testes.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Initiate(
        [FromBody] InitiatePaymentRequestDto dto,
        [FromHeader] string? userId)
    {
        // In production, APIM validates the user and passes user info via headers
        // In development, userId can be passed via header for testing
        Guid userGuid;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out userGuid))
        {
            // For development/testing without APIM, use a default user ID
            userGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
            _logger.LogWarning("No userId provided, using default for development: {UserId}", userGuid);
        }

        _logger.LogInformation("Initiating payment. UserId={UserId}, GameId={GameId}, Amount={Amount}", 
            userGuid, dto.GameId, dto.Amount);
        
        var resp = await _mediator.Send(new InitiatePaymentCommand(userGuid, dto));
        
        _logger.LogInformation("Payment created with id {PaymentId} and status {Status}.", 
            resp.PaymentId, resp.Status);
        
        return Accepted($"/api/payments/{resp.PaymentId}/status", new { paymentId = resp.PaymentId });
    }

    /// <summary>
    /// Obtém o status de um pagamento
    /// </summary>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Status(Guid id)
    {
        _logger.LogInformation("Getting payment status for {PaymentId}.", id);
        
        try
        {
            var resp = await _mediator.Send(new GetPaymentStatusQuery(id));
            _logger.LogInformation("Payment {PaymentId} status is {Status}.", id, resp.Status);
            return Ok(new { status = resp.Status });
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Payment {PaymentId} not found.", id);
            return NotFound(new { mensagem = "Pagamento não encontrado" });
        }
    }
}
