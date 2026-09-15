using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Customers.Commands.CreateCustomer;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Customers;
using MechanicShop.Tests.Common.Customers;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Customers.Commands.CreateCustomer;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateCustomerCommandTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateCustomerAndVehiclesSuccessfully()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var vehicleCommand = new CreateVehicleCommand("Tesla", "Model 3", 2024, "ELC-999");
        var command = new CreateCustomerCommand(
            FirstName: "Elon",
            LastName: "Musk",
            PhoneNumber: "01229293123",
            Email: "elon@tesla.com",
            Vehicles: [vehicleCommand]);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("Elon Musk");
        result.Value.Email.Should().Be("elon@tesla.com");

        // Assert against database using isolated assertion scope
        using var assertScope = _factory.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var dbCustomer = await assertContext.Customers
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == result.Value.CustomerId);

        dbCustomer.Should().NotBeNull();
        dbCustomer!.Name.FullName.Should().Be("Elon Musk");
        dbCustomer.Email.Value.Should().Be("elon@tesla.com");
        dbCustomer.PhoneNumber.Value.Should().Be("01229293123");

        dbCustomer.Vehicles.Should().ContainSingle();
        dbCustomer.Vehicles.First().Make.Should().Be("Tesla");
        dbCustomer.Vehicles.First().Model.Should().Be("Model 3");
        dbCustomer.Vehicles.First().Year.Should().Be(2024);
        dbCustomer.Vehicles.First().LicensePlate.Should().Be("ELC-999");
    }

    [Fact]
    public async Task Handle_WhenCustomerEmailAlreadyExists_ShouldReturnDuplicateError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var existingEmail = "duplicate@gmail.com";

        // Seed an existing customer
        var existingCustomer = CustomerFactory.CreateCustomer(
            firstName: "Existing",
            lastName: "User",
            phoneNumber: "01229293123",
            email: existingEmail).Value;

        await context.Customers.AddAsync(existingCustomer);
        await context.SaveChangesAsync(default);

        var command = new CreateCustomerCommand(
            FirstName: "New",
            LastName: "User",
            PhoneNumber: "01229293122",
            Email: existingEmail, // Duplicate Email
            Vehicles: [new CreateVehicleCommand("Ford", "Mustang", 2021, "FRD-111")]);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Customer_Email_Exists");
    }
}