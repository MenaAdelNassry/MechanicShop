using FluentValidation.TestHelper;

using MechanicShop.Application.Features.Customers.Commands.UpdateCustomer;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommandValidatorTests
{
    private readonly UpdateCustomerCommandValidator _validator;

    public UpdateCustomerCommandValidatorTests()
    {
        _validator = new UpdateCustomerCommandValidator();
    }

    [Fact]
    public void Validator_WhenCustomerIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new UpdateCustomerCommand(
            CustomerId: Guid.Empty,
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: "+123456789",
            Email: "john.doe@gmail.com",
            Vehicles: [new UpdateVehicleCommand(null, "Toyota", "Camry", 2022, "123-ABC")]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void Validator_WhenNamesAreEmpty_ShouldHaveValidationErrors()
    {
        var command = new UpdateCustomerCommand(
            CustomerId: Guid.CreateVersion7(),
            FirstName: string.Empty,
            LastName: string.Empty,
            PhoneNumber: "+123456789",
            Email: "john.doe@gmail.com",
            Vehicles: [new UpdateVehicleCommand(null, "Toyota", "Camry", 2022, "123-ABC")]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FirstName).WithErrorMessage("First name is required");
        result.ShouldHaveValidationErrorFor(x => x.LastName).WithErrorMessage("Last name is required");
    }

    [Fact]
    public void Validator_WhenVehiclesListIsEmpty_ShouldHaveValidationError()
    {
        var command = new UpdateCustomerCommand(
            CustomerId: Guid.CreateVersion7(),
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: "+123456789",
            Email: "john.doe@gmail.com",
            Vehicles: new List<UpdateVehicleCommand>());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Vehicles)
              .WithErrorMessage("At least one vehicle is required.");
    }

    [Fact]
    public void Validator_WhenNestedVehiclePropertiesAreInvalid_ShouldTriggerNestedVehicleValidator()
    {
        var command = new UpdateCustomerCommand(
            CustomerId: Guid.CreateVersion7(),
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: "+123456789",
            Email: "john.doe@gmail.com",
            Vehicles: [new UpdateVehicleCommand(null, string.Empty, "Model S", 2023, string.Empty)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Vehicles[0].Make");
        result.ShouldHaveValidationErrorFor("Vehicles[0].LicensePlate");
    }

    [Fact]
    public void Validator_WhenAllDataIsValid_ShouldNotHaveValidationErrors()
    {
        var command = new UpdateCustomerCommand(
            CustomerId: Guid.CreateVersion7(),
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: "+123456789",
            Email: "john.doe@gmail.com",
            Vehicles: [new UpdateVehicleCommand(null, "Toyota", "Camry", 2022, "123-ABC")]);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}