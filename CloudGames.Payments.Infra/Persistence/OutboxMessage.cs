namespace CloudGames.Payments.Infra.Persistence;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
}
