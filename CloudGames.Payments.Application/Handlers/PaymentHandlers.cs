using System.Text.Json;
using CloudGames.Payments.Application.DTOs;
using CloudGames.Payments.Application.Commands;
using CloudGames.Payments.Application.Queries;
using CloudGames.Payments.Domain.Entities;
using CloudGames.Payments.Domain.Events;
using CloudGames.Payments.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CloudGames.Payments.Application.Handlers;

public class InitiatePaymentHandler : IRequestHandler<InitiatePaymentCommand, PaymentResponseDto>
{
    private readonly IPaymentRepository _repository;
    private readonly IEventStore _eventStore;
    private readonly ILogger<InitiatePaymentHandler> _logger;

    public InitiatePaymentHandler(IPaymentRepository repository, IEventStore eventStore, ILogger<InitiatePaymentHandler> logger)
    {
        _repository = repository;
        _eventStore = eventStore;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> Handle(InitiatePaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Criando pagamento. UserId={UserId}, GameId={GameId}, Amount={Amount}", request.UserId, request.Request.GameId, request.Request.Amount);
        var payment = Payment.Create(request.UserId, request.Request.GameId, request.Request.Amount);
        // Não aprova automaticamente; permanece Pending até o processador de confirmação atualizar
        await _repository.AddAsync(payment, cancellationToken);

        foreach (var evt in payment.DomainEvents)
        {
            var type = evt.GetType().Name;
            var data = JsonSerializer.Serialize(evt);
            await _eventStore.AppendAsync(new StoredEvent
            {
                AggregateId = payment.Id,
                Type = type,
                Data = data,
                OccurredOn = DateTime.UtcNow
            }, cancellationToken);
        }
        payment.ClearDomainEvents();

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Pagamento persistido. Id={PaymentId}, Status={Status}", payment.Id, payment.Status);
        return new PaymentResponseDto(payment.Id, payment.Status.ToString());
    }
}

public class GetPaymentStatusHandler : IRequestHandler<GetPaymentStatusQuery, PaymentResponseDto>
{
    private readonly IPaymentRepository _repository;

    public GetPaymentStatusHandler(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentResponseDto> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        var p = await _repository.GetByIdAsync(request.PaymentId, cancellationToken)
                ?? throw new KeyNotFoundException("Pagamento não encontrado");
        return new PaymentResponseDto(p.Id, p.Status.ToString());
    }
}
