using System.Text.RegularExpressions;

using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Common.ValueObjects;

public sealed partial record PhoneNumber
{
    // We are using GeneratedRegex Attr to improve performance and avoid regex compilation or parsing at runtime.
    [GeneratedRegex(@"^01[0125][0-9]{8}$")]
    private static partial Regex EgyptianPhoneRegex();

    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    public static Result<PhoneNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Error.Validation("Phone.Required", "Phone number is required.");
        }

        var cleanedPhone = value.Trim();

        if (!EgyptianPhoneRegex().IsMatch(cleanedPhone))
        {
            return Error.Validation("Phone.Invalid", "Phone number must be a valid 11-digit Egyptian mobile number (starting with 010, 011, 012, or 015).");
        }

        return new PhoneNumber(cleanedPhone);
    }

    public override string ToString() => Value;
}