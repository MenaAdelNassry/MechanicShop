using FluentAssertions;

using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Tests.Common;
using MechanicShop.Tests.Common.Customers;

using Xunit;

namespace MechanicShop.Domain.UnitTests.Customers;

public class CustomerTests
{
    [Fact]
    public void CreateCustomer_ShouldSucceed_WithValidData()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        const string firstName = "Customer";
        const string lastName = "One";
        const string phoneNumber = "01123456789";
        const string email = "customer01@localhost.com";

        // Act
        var result = Customer.Create(
            id: id,
            name: PersonName.Create(firstName, lastName).Value,
            phoneNumber: PhoneNumber.Create(phoneNumber).Value,
            email: EmailAddress.Create(email).Value,
            vehicles: []);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var customer = result.Value;
        customer.Should().NotBeNull();
        customer.Id.Should().Be(id);
        customer.Name.FirstName.Should().Be(firstName);
        customer.Name.LastName.Should().Be(lastName);
        customer.PhoneNumber.Value.Should().Be(phoneNumber);
        customer.Email.Value.Should().Be(email);
        customer.Vehicles.Should().BeEmpty();
    }

    [Fact]
    public void UpdateCustomer_ShouldSucceed_WithValidData()
    {
        // Arrange
        var customer = CustomerFactory.CreateCustomer().Value;
        var personName = PersonName.Create("Mena", "Adel").Value;
        var email = EmailAddress.Create("updated@email.com").Value;
        var phone = PhoneNumber.Create("01234567890").Value;

        // Act
        var result = customer.Update(personName, email, phone);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Result.Updated);
        customer.Name.FirstName.Should().Be("Mena");
    }

    [Fact]
    public void SyncVehicles_ShouldAddNewVehiclesAndUpdateExisting()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.UtcNow);

        var customer = CustomerFactory.CreateCustomer().Value;
        var addResult = customer.UpsertVehicles(
            [
            new VehicleData(null, "Ford", "Focus", 2020, "ABC-1234")
        ], fakeTime);

        addResult.IsSuccess.Should().BeTrue();

        var existingVehicleId = customer.Vehicles.First().Id;

        var updatedVehicleData = new VehicleData(existingVehicleId, "UpdatedFord", "Focus", 2021, "ABC-1234");
        var newVehicleData = new VehicleData(null, "NewBrand", "Corolla", 2023, "XYZ-9999");

        // Act
        var result = customer.UpsertVehicles([updatedVehicleData, newVehicleData], fakeTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Result.Updated);
        customer.Vehicles.Should().HaveCount(2);
        customer.Vehicles.Should().Contain(v => v.Id == existingVehicleId && v.Make == "UpdatedFord" && v.Year == 2021);
        customer.Vehicles.Should().Contain(v => v.Make == "NewBrand" && v.Model == "Corolla");
    }

    [Fact]
    public void SyncVehicles_ShouldRemoveVehiclesNotInIncomingList()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.UtcNow);

        var customer = CustomerFactory.CreateCustomer().Value;

        customer.UpsertVehicles(
            [
            new VehicleData(null, "Toyota", "Yaris", 2019, "TOY-1111"),
            new VehicleData(null, "Honda", "Civic", 2022, "HON-2222")
        ], fakeTime);

        customer.Vehicles.Should().HaveCount(2);
        var keptVehicleId = customer.Vehicles.Last().Id;

        var incoming = new VehicleData(keptVehicleId, "Honda", "Civic", 2022, "HON-2222");

        // Act
        var result = customer.UpsertVehicles([incoming], fakeTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Result.Updated);
        customer.Vehicles.Should().ContainSingle();
        customer.Vehicles.First().Id.Should().Be(keptVehicleId);
    }
}