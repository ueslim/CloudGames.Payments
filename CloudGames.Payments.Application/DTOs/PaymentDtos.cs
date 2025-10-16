namespace CloudGames.Payments.Application.DTOs;

// UserId removed from request - comes from APIM via header
public record InitiatePaymentRequestDto(Guid GameId, decimal Amount);
public record PaymentResponseDto(Guid PaymentId, string Status);
