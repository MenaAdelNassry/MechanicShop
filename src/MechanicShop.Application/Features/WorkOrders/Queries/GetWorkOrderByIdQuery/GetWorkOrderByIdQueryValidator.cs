using FluentValidation;

namespace MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrderByIdQuery;

public sealed class GetWorkOrderByIdQueryValidator : AbstractValidator<GetWorkOrderByIdQuery>
{
    public GetWorkOrderByIdQueryValidator()
    {
        RuleFor(request => request.WorkOrderId)
            .NotEmpty()
            .WithErrorCode("WorkOrderId_Is_Required")
            .WithMessage("WorkOrderId is required.");
    }
}