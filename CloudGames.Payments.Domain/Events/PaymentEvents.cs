namespace CloudGames.Payments.Domain.Events;

public record PaymentApproved(Guid PaymentId, Guid UserId, Guid GameId, decimal Amount);

public record PaymentDeclined(Guid PaymentId, Guid UserId, Guid GameId, decimal Amount, string Reason);
