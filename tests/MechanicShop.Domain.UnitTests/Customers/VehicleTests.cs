using System;

using FluentAssertions;

using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Tests.Common;
using MechanicShop.Tests.Common.Customers;

using Xunit;

namespace MechanicShop.Domain.UnitTests.Customers;

public class VehicleTests
{
    [Fact]
    public void CreateVehicle_ShouldSucceed_WithValidData()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        const string make = "Honda";
        const string model = "Accord";
        const int year = 2024;
        const string licensePlate = "ABC 123";

        // Act
        var result = VehicleFactory.CreateVehicle(id: id, make: make, model: model, year: year, licensePlate: licensePlate);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var vehicle = result.Value;
        vehicle.Make.Should().Be(make);
        vehicle.Model.Should().Be(model);
        vehicle.Year.Should().Be(year);
        vehicle.LicensePlate.Should().Be(licensePlate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateVehicle_ShouldFail_WhenMakeInvalid(string make)
    {
        // Act
        var result = VehicleFactory.CreateVehicle(make: make);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(VehicleErrors.MakeRequired.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateVehicle_ShouldFail_WhenModelInvalid(string model)
    {
        // Act
        var result = VehicleFactory.CreateVehicle(model: model);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(VehicleErrors.ModelRequired.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateVehicle_ShouldFail_WhenLicensePlateInvalid(string plate)
    {
        // Act
        var result = VehicleFactory.CreateVehicle(licensePlate: plate);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(VehicleErrors.LicensePlateRequired.Code);
    }

    [Theory]
    [InlineData(1700)]
    [InlineData(3000)]
    public void CreateVehicle_ShouldFail_WhenYearInvalid(int year)
    {
        // Act
        var result = VehicleFactory.CreateVehicle(year: year);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(VehicleErrors.YearInvalid.Code);
    }

    [Fact]
    public void UpdateVehicle_ShouldSucceed_WithValidData()
    {
        // Arrange
        var vehicle = VehicleFactory.CreateVehicle().Value;
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.Parse("2026-01-01"));

        // Act
        var result = vehicle.Update("Toyota", "Camry", 2022, "XYZ 789", fakeTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        vehicle.Make.Should().Be("Toyota");
        vehicle.Model.Should().Be("Camry");
        vehicle.Year.Should().Be(2022);
        vehicle.LicensePlate.Should().Be("XYZ 789");
    }

    [Fact]
    public void UpdateVehicle_ShouldFail_WhenMakeIsInvalid()
    {
        // Arrange
        var vehicle = VehicleFactory.CreateVehicle().Value;
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.Parse("2026-01-01"));

        // Act
        var result = vehicle.Update(string.Empty, "Model", 2022, "XYZ123", fakeTime);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(VehicleErrors.MakeRequired.Code);
    }

    [Fact]
    public void UpdateVehicle_ShouldFail_WhenModelIsInvalid()
    {
        // Arrange
        var vehicle = VehicleFactory.CreateVehicle().Value;
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.Parse("2026-01-01"));

        // Act
        var result = vehicle.Update("Make", string.Empty, 2022, "XYZ123", fakeTime);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(VehicleErrors.ModelRequired.Code);
    }

    [Fact]
    public void UpdateVehicle_ShouldFail_WhenLicensePlateIsInvalid()
    {
        // Arrange
        var vehicle = VehicleFactory.CreateVehicle().Value;
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.Parse("2026-01-01"));

        // Act
        var result = vehicle.Update("Make", "Model", 2022, string.Empty, fakeTime);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(VehicleErrors.LicensePlateRequired.Code);
    }

    [Theory]
    [InlineData(1800)]
    [InlineData(5000)]
    public void UpdateVehicle_ShouldFail_WhenYearInvalid(int year)
    {
        // Arrange
        var vehicle = VehicleFactory.CreateVehicle().Value;
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.Parse("2026-01-01"));

        // Act
        var result = vehicle.Update("Make", "Model", year, "XYZ123", fakeTime);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(VehicleErrors.YearInvalid.Code);
    }

    [Fact]
    public void VehicleInfo_ShouldReturnFormattedString()
    {
        // Act
        var vehicle = VehicleFactory.CreateVehicle(make: "Ford", model: "Mustang", year: 2021).Value;

        // Assert
        vehicle.VehicleInfo.Should().Be("Ford | Mustang | 2021");
    }
}