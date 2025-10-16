namespace CloudGames.Payments.Application.DTOs;

public record InitiatePaymentRequestDto(Guid GameId, Guid? UserId, decimal Amount);
public record PaymentResponseDto(Guid PaymentId, string Status);
