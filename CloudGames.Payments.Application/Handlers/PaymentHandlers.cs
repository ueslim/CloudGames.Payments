using System.Text.Json;
using CloudGames.Payments.Application.DTOs;
using CloudGames.Payments.Application.Commands;
using CloudGames.Payments.Application.Queries;
using CloudGames.Payments.Domain.Entities;
using CloudGames.Payments.Domain.Events;
using CloudGames.Payments.Domain.Repositories;
using MediatR;

namespace CloudGames.Payments.Application.Handlers;

public class InitiatePaymentHandler : IRequestHandler<InitiatePaymentCommand, PaymentResponseDto>
{
    private readonly IPaymentRepository _repository;
    private readonly IEventStore _eventStore;

    public InitiatePaymentHandler(IPaymentRepository repository, IEventStore eventStore)
    {
        _repository = repository;
        _eventStore = eventStore;
    }

    public async Task<PaymentResponseDto> Handle(InitiatePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = Payment.Create(request.UserId, request.Request.GameId, request.Request.Amount);
        // For now, approve immediately to simulate gateway confirmation
        payment.Approve();

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
                ?? throw new KeyNotFoundException("Payment not found");
        return new PaymentResponseDto(p.Id, p.Status.ToString());
    }
}
