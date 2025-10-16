namespace CloudGames.Payments.Infra.Persistence;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow; // mapped to OccurredAt
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedOn { get; set; } // mapped to ProcessedAt
    public int AttemptCount { get; set; }
}
