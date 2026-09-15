using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Customers.Vehicles;

namespace MechanicShop.Tests.Common.Customers;

public static class CustomerFactory
{
    public static Result<Customer> CreateCustomer(
        Guid? id = null,
        string? firstName = null,
        string? lastName = null,
        string? phoneNumber = null,
        string? email = null,
        List<Vehicle>? vehicles = null)
    {
        // Using valid default values compliant with newly introduced Egyptian phone & email validation rules
        var nameResult = PersonName.Create(firstName ?? "John", lastName ?? "Doe");
        var emailResult = EmailAddress.Create(email ?? "customer01@localhost.com");
        var phoneResult = PhoneNumber.Create(phoneNumber ?? "01023456789");

        if (vehicles is null || vehicles.Count == 0)
        {
            var defaultVehicle = VehicleFactory.CreateVehicle().Value;

            vehicles = [defaultVehicle];
        }

        return Customer.Create(
            id ?? Guid.CreateVersion7(),
            nameResult.Value,
            emailResult.Value,
            phoneResult.Value,
            vehicles);
    }
}