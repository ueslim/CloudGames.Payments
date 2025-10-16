namespace CloudGames.Payments.Domain.Entities;

public enum PaymentStatus { Pending, Approved, Declined }

public class Payment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid GameId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public static Payment Create(Guid userId, Guid gameId, decimal amount)
    {
        var payment = new Payment
        {
            UserId = userId,
            GameId = gameId,
            Amount = amount,
            Status = PaymentStatus.Pending
        };
        return payment;
    }

    public void Approve()
    {
        if (Status != PaymentStatus.Pending) return;
        Status = PaymentStatus.Approved;
        _domainEvents.Add(new Events.PaymentApproved(Id, UserId, GameId, Amount));
    }

    public void Decline(string reason)
    {
        if (Status != PaymentStatus.Pending) return;
        Status = PaymentStatus.Declined;
        _domainEvents.Add(new Events.PaymentDeclined(Id, UserId, GameId, Amount, reason));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
