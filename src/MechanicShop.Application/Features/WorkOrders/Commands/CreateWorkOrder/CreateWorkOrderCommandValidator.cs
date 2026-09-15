using FluentValidation;

namespace MechanicShop.Application.Features.WorkOrders.Commands.CreateWorkOrder;

public sealed class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("VehicleId is required.");

        RuleFor(request => request.StartAt)
            .GreaterThan(_ => DateTimeOffset.UtcNow)
            .WithMessage("StartAt must be in the future.");

        RuleFor(request => request.RepairTaskIds)
            .NotEmpty()
            .WithMessage("At least one repair task must be selected")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("RepairTaskIds contains an invalid empty GUID.");

        RuleFor(request => request.LaborId)
            .Must(laborId => laborId is null || laborId != Guid.Empty)
            .WithMessage("If provided, LaborId must not be empty.");

        RuleFor(x => x.Spot)
            .IsInEnum()
            .WithErrorCode("Spot_Invalid")
            .WithMessage("Spot must be a valid Spot value. [A, B, C, D]");
    }
}