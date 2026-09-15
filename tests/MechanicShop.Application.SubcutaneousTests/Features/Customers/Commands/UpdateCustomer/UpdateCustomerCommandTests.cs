using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Customers.Commands.UpdateCustomer;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Tests.Common.Customers;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Customers.Commands.UpdateCustomer;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpdateCustomerCommandTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateCustomerAndUpsertVehiclesCorrectly()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        // Create initial vehicles and customer
        var newCustomerId = Guid.CreateVersion7();
        var vehicle1 = VehicleFactory.CreateVehicle(Guid.CreateVersion7(), "Honda", "Civic", 2018, "HND", newCustomerId, timeProvider).Value;
        var vehicleToRemoved = VehicleFactory.CreateVehicle(Guid.CreateVersion7(), "Ford", "Focus", 2015, "FRD", newCustomerId, timeProvider).Value;

        var customer = CustomerFactory.CreateCustomer(
            id: newCustomerId,
            firstName: "Alice",
            lastName: "Smith",
            email: "alice@smith.com",
            phoneNumber: "01229293524",
            vehicles: [vehicle1, vehicleToRemoved]).Value;

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        // We prepare the update payload:
        // 1. Update details of vehicle1
        var updatedVehicle1Command = new UpdateVehicleCommand(vehicle1.Id, "Honda", "Accord", 2020, "HBC");

        // 2. Insert a brand-new vehicle
        var newVehicleCommand = new UpdateVehicleCommand(null, "Tesla", "Model Y", 2023, "TSL");

        // Note: vehicleToRemoved is omitted from this list, which should cause it to be deleted.
        var command = new UpdateCustomerCommand(
            CustomerId: customer.Id,
            FirstName: "Alicia",
            LastName: "Smith-Jones",
            PhoneNumber: "01229293123",
            Email: "alicia@smithjones.com",
            Vehicles: new List<UpdateVehicleCommand> { updatedVehicle1Command, newVehicleCommand });

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var assertScope = _factory.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var dbCustomer = await assertContext.Customers
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == customer.Id);

        dbCustomer.Should().NotBeNull();
        dbCustomer!.Name.FullName.Should().Be("Alicia Smith-Jones");
        dbCustomer.Email.Value.Should().Be("alicia@smithjones.com");
        dbCustomer.PhoneNumber.Value.Should().Be("01229293123");

        // Verify Upserting: Expecting exactly 2 vehicles (vehicle1 updated, newVehicle added, vehicleToRemoved deleted)
        dbCustomer.Vehicles.Should().HaveCount(2);

        var dbVehicle1 = dbCustomer.Vehicles.FirstOrDefault(v => v.Id == vehicle1.Id);
        dbVehicle1.Should().NotBeNull();
        dbVehicle1!.Model.Should().Be("Accord");
        dbVehicle1.Year.Should().Be(2020);
        dbVehicle1.LicensePlate.Should().Be("HBC");

        var dbNewVehicle = dbCustomer.Vehicles.FirstOrDefault(v => v.Id != vehicle1.Id);
        dbNewVehicle.Should().NotBeNull();
        dbNewVehicle!.Make.Should().Be("Tesla");
        dbNewVehicle.Model.Should().Be("Model Y");
        dbNewVehicle.Year.Should().Be(2023);
        dbNewVehicle.LicensePlate.Should().Be("TSL");

        // Ensure the omitted vehicle was removed from database storage
        var dbOmittedVehicleExist = await assertContext.Vehicles.AnyAsync(v => v.Id == vehicleToRemoved.Id);
        dbOmittedVehicleExist.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyBelongsToAnotherCustomer_ShouldReturnEmailConflictError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var otherEmailStr = "occupied@gmail.com";
        var otherCustomer = CustomerFactory.CreateCustomer(
            firstName: "Bob",
            lastName: "Johnson",
            email: otherEmailStr,
            phoneNumber: "01229293524").Value;

        var targetCustomer = CustomerFactory.CreateCustomer(
            firstName: "Alice",
            lastName: "Smith",
            email: "alice@smith.com",
            phoneNumber: "01229293522").Value;

        await context.Customers.AddRangeAsync(otherCustomer, targetCustomer);
        await context.SaveChangesAsync(default);

        var command = new UpdateCustomerCommand(
            CustomerId: targetCustomer.Id,
            FirstName: "Alice",
            LastName: "Smith",
            PhoneNumber: "01229293333",
            Email: otherEmailStr, // Conflicts with Bob's email
            Vehicles: [new UpdateVehicleCommand(null, "Ford", "F-150", 2021, "FRD-123")]);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Customer_Email_Exist");
    }

    [Fact]
    public async Task Handle_WhenCustomerDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpdateCustomerCommand(
            CustomerId: Guid.CreateVersion7(), // Random non-existent ID
            FirstName: "Ghost",
            LastName: "User",
            PhoneNumber: "+123456789",
            Email: "ghost@gmail.com",
            Vehicles: [new UpdateVehicleCommand(null, "Toyota", "Supra", 2022, "SPR-99")]);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("ApplicationErrors.Customer.NotFound");
    }
}