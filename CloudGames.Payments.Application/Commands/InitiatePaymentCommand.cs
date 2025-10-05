using CloudGames.Payments.Application.DTOs;
using MediatR;
using FluentValidation;

namespace CloudGames.Payments.Application.Commands;

public record InitiatePaymentCommand(Guid UserId, InitiatePaymentRequestDto Request) : IRequest<PaymentResponseDto>;

public class InitiatePaymentCommandValidator : AbstractValidator<InitiatePaymentCommand>
{
    public InitiatePaymentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request.GameId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0);
    }
}
