using CloudGames.Payments.Application.DTOs;
using MediatR;
using FluentValidation;

namespace CloudGames.Payments.Application.Queries;

public record GetPaymentStatusQuery(Guid PaymentId) : IRequest<PaymentResponseDto>;

public class GetPaymentStatusQueryValidator : AbstractValidator<GetPaymentStatusQuery>
{
    public GetPaymentStatusQueryValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
    }
}
