using FluentValidation;

namespace MechanicShop.Application.Features.WorkOrders.Commands.CancelWorkOrder;

public sealed class CancelWorkOrderCommandValidator : AbstractValidator<CancelWorkOrderCommand>
{
    public CancelWorkOrderCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty()
            .WithErrorCode("WorkOrderId_Required")
            .WithMessage("WorkOrderId is required.");
    }
}