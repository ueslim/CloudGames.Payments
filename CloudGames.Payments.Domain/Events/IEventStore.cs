namespace CloudGames.Payments.Domain.Events;

public interface IEventStore
{
    Task AppendAsync(StoredEvent storedEvent, CancellationToken ct = default);
}

public class StoredEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AggregateId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
}
