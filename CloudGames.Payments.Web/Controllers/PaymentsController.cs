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
        // Em produção, APIM valida o usuário e passa informações via headers
        // Em desenvolvimento, userId pode ser passado via header para testes
        Guid userGuid;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out userGuid))
        {
            // Para desenvolvimento/testes sem APIM, usa um ID de usuário padrão
            userGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
            _logger.LogWarning("Nenhum userId fornecido, usando padrão para desenvolvimento: {UserId}", userGuid);
        }

        _logger.LogInformation("Iniciando pagamento. UserId={UserId}, GameId={GameId}, Amount={Amount}", 
            userGuid, dto.GameId, dto.Amount);
        
        var resp = await _mediator.Send(new InitiatePaymentCommand(userGuid, dto));
        
        _logger.LogInformation("Pagamento criado com id {PaymentId} e status {Status}.", 
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
        _logger.LogInformation("Obtendo status do pagamento {PaymentId}.", id);
        
        try
        {
            var resp = await _mediator.Send(new GetPaymentStatusQuery(id));
            _logger.LogInformation("Status do pagamento {PaymentId} é {Status}.", id, resp.Status);
            return Ok(new { status = resp.Status });
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Pagamento {PaymentId} não encontrado.", id);
            return NotFound(new { mensagem = "Pagamento não encontrado" });
        }
    }
}
