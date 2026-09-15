using FluentValidation;

using MechanicShop.Application.Features.Billing.Commands.IssueInvoice;

public sealed class IssueInvoiceCommandValidator : AbstractValidator<IssueInvoiceCommand>
{
    public IssueInvoiceCommandValidator()
    {
        RuleFor(request => request.WorkOrderId)
            .NotEmpty()
            .WithErrorCode("WorkOrderId_Is_Required")
            .WithMessage("WorkOrderId is required.");

        When(request => request.DiscountAmount.HasValue, () =>
        {
            RuleFor(request => request.DiscountAmount!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Discount amount cannot be negative.");
        });
    }
}