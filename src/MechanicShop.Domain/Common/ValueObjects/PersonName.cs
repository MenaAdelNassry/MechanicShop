using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Common.ValueObjects;

public sealed record PersonName
{
    public const int MaxLength = 50;
    public const int MinLength = 2;

    public string FirstName { get; }
    public string LastName { get; }
    public string FullName => $"{FirstName} {LastName}";

    private PersonName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static Result<PersonName> Create(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Error.Validation("Name.FirstNameRequired", "First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Error.Validation("Name.LastNameRequired", "Last name is required.");
        }

        var trimmedFirst = firstName.Trim();
        var trimmedLast = lastName.Trim();

        if (trimmedFirst.Length < MinLength || trimmedFirst.Length > MaxLength)
        {
            return Error.Validation("Name.FirstNameInvalidLength", $"First name must be between {MinLength} and {MaxLength} characters.");
        }

        if (trimmedLast.Length < MinLength || trimmedLast.Length > MaxLength)
        {
            return Error.Validation("Name.LastNameInvalidLength", $"Last name must be between {MinLength} and {MaxLength} characters.");
        }

        return new PersonName(trimmedFirst, trimmedLast);
    }

    public override string ToString() => FullName;
}