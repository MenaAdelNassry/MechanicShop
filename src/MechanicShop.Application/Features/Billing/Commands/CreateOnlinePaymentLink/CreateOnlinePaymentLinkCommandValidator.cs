using FluentValidation;

namespace MechanicShop.Application.Features.Billing.Commands.CreateOnlinePaymentLink;

public sealed class CreateOnlinePaymentLinkCommandValidator : AbstractValidator<CreateOnlinePaymentLinkCommand>
{
    public CreateOnlinePaymentLinkCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage("InvoiceId is required.");

        RuleFor(x => x.SuccessUrl)
            .NotEmpty().WithMessage("Success URL is required.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Success URL must be a valid absolute URI.");

        RuleFor(x => x.CancelUrl)
            .NotEmpty().WithMessage("Cancel URL is required.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Cancel URL must be a valid absolute URI.");
    }
}