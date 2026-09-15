using FluentAssertions;

using MechanicShop.Application.Features.Customers.Mappers;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Tests.Common.Customers;

using Xunit;

namespace MechanicShop.Application.UnitTests.Mappers;

public class CustomerMapperTest
{
    [Fact]
    public void ToDto_WithValidCustomerEntity_ShouldMapCorrectly()
    {
        // Arrange
        var customer = CustomerFactory.CreateCustomer(
            firstName: "John",
            lastName: "Doe",
            email: "john.doe@example.com",
            phoneNumber: "01023456789").Value;

        var expectedVehicle = customer.Vehicles.First();

        // Act
        var dto = customer.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.CustomerId.Should().Be(customer.Id);
        dto.Name.Should().Be(customer.Name.FullName);
        dto.Email.Should().Be(customer.Email.Value);
        dto.PhoneNumber.Should().Be(customer.PhoneNumber.Value);

        dto.Vehicles.Should().ContainSingle();
        var vehicleDto = dto.Vehicles[0];
        vehicleDto.VehicleId.Should().Be(expectedVehicle.Id);
        vehicleDto.Make.Should().Be(expectedVehicle.Make);
        vehicleDto.Model.Should().Be(expectedVehicle.Model);
        vehicleDto.Year.Should().Be(expectedVehicle.Year);
        vehicleDto.LicensePlate.Should().Be(expectedVehicle.LicensePlate);
    }

    [Fact]
    public void ToDtos_WithCustomerEnumerable_ShouldMapListCorrectly()
    {
        // Arrange
        var customer = CustomerFactory.CreateCustomer().Value;
        var entities = new List<Customer> { customer };

        // Act
        var dtos = entities.ToDtos();

        // Assert
        dtos.Should().ContainSingle();
        dtos[0].CustomerId.Should().Be(customer.Id);
        dtos[0].Name.Should().Be(customer.Name.FullName);
    }

    [Fact]
    public void ToDto_WithNullCustomer_ShouldThrowArgumentNullException()
    {
        // Arrange
        Customer? customer = null;

        // Act
        Action act = () => customer!.ToDto();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDtos_WithNullCustomerEnumerable_ShouldThrowArgumentNullException()
    {
        // Arrange
        List<Customer>? entities = null;

        // Act
        Action act = () => entities!.ToDtos();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDto_WithValidVehicleEntity_ShouldMapCorrectly()
    {
        // Arrange
        var vehicle = VehicleFactory.CreateVehicle(
            make: "Toyota",
            model: "Corolla",
            year: 2022,
            licensePlate: "XYZ 789").Value;

        // Act
        var dto = vehicle.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.VehicleId.Should().Be(vehicle.Id);
        dto.Make.Should().Be(vehicle.Make);
        dto.Model.Should().Be(vehicle.Model);
        dto.Year.Should().Be(vehicle.Year);
        dto.LicensePlate.Should().Be(vehicle.LicensePlate);
    }

    [Fact]
    public void ToDtos_WithVehicleEnumerable_ShouldMapListCorrectly()
    {
        // Arrange
        var vehicle = VehicleFactory.CreateVehicle().Value;
        var entities = new List<Vehicle> { vehicle };

        // Act
        var dtos = entities.ToDtos();

        // Assert
        dtos.Should().ContainSingle();
        dtos[0].VehicleId.Should().Be(vehicle.Id);
    }

    [Fact]
    public void ToDto_WithNullVehicle_ShouldThrowArgumentNullException()
    {
        // Arrange
        Vehicle? vehicle = null;

        // Act
        Action act = () => vehicle!.ToDto();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDtos_WithNullVehicleEnumerable_ShouldThrowArgumentNullException()
    {
        // Arrange
        List<Vehicle>? entities = null;

        // Act
        Action act = () => entities!.ToDtos();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}