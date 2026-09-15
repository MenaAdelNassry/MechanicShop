using FluentValidation.TestHelper;

using MechanicShop.Application.Features.Customers.Commands.CreateCustomer;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandValidatorTests
{
    private readonly CreateCustomerCommandValidator _validator;

    public CreateCustomerCommandValidatorTests()
    {
        _validator = new CreateCustomerCommandValidator();
    }

    [Fact]
    public void Validator_WhenNamesAreEmpty_ShouldHaveValidationErrors()
    {
        var command = new CreateCustomerCommand(
            FirstName: string.Empty,
            LastName: string.Empty,
            PhoneNumber: "+123456789",
            Email: "customer@gmail.com",
            Vehicles: [new CreateVehicleCommand("Toyota", "Camry", 2022, "123-ABC")]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FirstName).WithErrorMessage("First name is required");
        result.ShouldHaveValidationErrorFor(x => x.LastName).WithErrorMessage("Last name is required");
    }

    [Fact]
    public void Validator_WhenVehiclesListIsEmpty_ShouldHaveValidationError()
    {
        var command = new CreateCustomerCommand(
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: "+123456789",
            Email: "customer@gmail.com",
            Vehicles: new List<CreateVehicleCommand>());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Vehicles)
              .WithErrorMessage("At least one vehicle is required.");
    }

    [Fact]
    public void Validator_WhenNestedVehiclePropertiesAreInvalid_ShouldTriggerVehicleValidator()
    {
        var command = new CreateCustomerCommand(
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: "+123456789",
            Email: "customer@gmail.com",
            Vehicles: [new CreateVehicleCommand(string.Empty, "Model S", 2023, string.Empty)]); // Empty Make and LicensePlate

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Vehicles[0].Make");
        result.ShouldHaveValidationErrorFor("Vehicles[0].LicensePlate");
    }

    [Fact]
    public void Validator_WhenAllDataIsValid_ShouldNotHaveValidationErrors()
    {
        var command = new CreateCustomerCommand(
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: "+1234567890",
            Email: "john.doe@gmail.com",
            Vehicles: [new CreateVehicleCommand("Toyota", "Camry", 2022, "123-ABC")]);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}