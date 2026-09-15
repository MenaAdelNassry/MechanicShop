using FluentValidation;

namespace MechanicShop.Application.Features.WorkOrders.Queries.GetPublicTrackingInfo;

public sealed class GetPublicTrackingInfoQueryValidator : AbstractValidator<GetPublicTrackingInfoQuery>
{
    public GetPublicTrackingInfoQueryValidator()
    {
        RuleFor(x => x.TrackingToken)
            .NotEmpty()
            .WithErrorCode("TrackingToken.Required")
            .WithMessage("Tracking token is required to view work order status.");
    }
}