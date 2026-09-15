using FluentValidation;

using MechanicShop.Domain.Workorders.Billing.Enums;

namespace MechanicShop.Application.Features.Billing.Commands.RecordPayment;

public sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage("InvoiceId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Payment amount exceeds allowable single transaction limit.");

        RuleFor(x => x.Method)
            .IsInEnum().WithMessage("A valid payment method must be selected.");

        When(x => x.Method == PaymentMethod.PosCard || x.Method == PaymentMethod.BankTransfer, () =>
        {
            RuleFor(x => x.TransactionReference)
                .NotEmpty().WithMessage("Transaction reference/receipt number is required for card and bank transfer payments.")
                .MaximumLength(200).WithMessage("Transaction reference cannot exceed 200 characters.");
        });
    }
}