using System.Text.Json;
using CloudGames.Payments.Domain.Events;
using CloudGames.Payments.Infra.Persistence;

namespace CloudGames.Payments.Infra.EventStore;

public class EventStoreRepository : IEventStore
{
    private readonly EventStoreSqlContext _eventDb;
    private readonly PaymentsDbContext _paymentsDb;

    public EventStoreRepository(EventStoreSqlContext eventDb, PaymentsDbContext paymentsDb)
    {
        _eventDb = eventDb;
        _paymentsDb = paymentsDb;
    }

    public async Task AppendAsync(StoredEvent storedEvent, CancellationToken ct = default)
    {
        await _eventDb.StoredEvents.AddAsync(storedEvent, ct);
        // Outbox message mirrors the domain event for publishing
        await _paymentsDb.OutboxMessages.AddAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            OccurredAt = storedEvent.OccurredOn,
            Type = storedEvent.Type,
            Payload = storedEvent.Data
        }, ct);
        await _eventDb.SaveChangesAsync(ct);
        await _paymentsDb.SaveChangesAsync(ct);
    }
}
