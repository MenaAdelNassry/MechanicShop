using System.Net.Mail;

using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Common.ValueObjects;

public sealed record EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static Result<EmailAddress> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Error.Validation("Email.Required", "Email address is required.");
        }

        string trimmedEmail = email.Trim().ToLowerInvariant();

        if (!MailAddress.TryCreate(trimmedEmail, out var parsedAddress) || parsedAddress.Address != trimmedEmail)
        {
            return Error.Validation("Email.Invalid", "Invalid email address format.");
        }

        return new EmailAddress(trimmedEmail);
    }

    public override string ToString() => Value;
}