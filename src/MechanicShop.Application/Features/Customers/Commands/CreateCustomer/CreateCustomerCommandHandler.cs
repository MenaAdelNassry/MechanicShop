using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Application.Features.Customers.Mappers;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Customers.Vehicles;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandHandler(
    ILogger<CreateCustomerCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider
    )
    : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand command, CancellationToken ct)
    {
        var nameResult = PersonName.Create(command.FirstName, command.LastName);
        if (nameResult.IsError) return nameResult.Errors;

        var phoneResult = PhoneNumber.Create(command.PhoneNumber);
        if (phoneResult.IsError) return phoneResult.Errors;

        var emailResult = EmailAddress.Create(command.Email);
        if (emailResult.IsError) return emailResult.Errors;

        // 1. Check if email or phone number already exists
        var duplicateCustomer = await context.Customers
            .Where(c => c.Email == emailResult.Value || c.PhoneNumber == phoneResult.Value)
            .Select(c => new
            {
                EmailMatches = c.Email == emailResult.Value,
                PhoneMatches = c.PhoneNumber == phoneResult.Value
            })
            .FirstOrDefaultAsync(ct);

        if (duplicateCustomer is not null)
        {
            if (duplicateCustomer.EmailMatches)
            {
                logger.LogWarning("Customer creation aborted. Email already exists: {Email}", command.Email);
                return Error.Conflict("Customer.DuplicateEmail", $"A customer with email '{command.Email}' already exists.");
            }

            if (duplicateCustomer.PhoneMatches)
            {
                logger.LogWarning("Customer creation aborted. Phone number already exists: {PhoneNumber}", command.PhoneNumber);
                return Error.Conflict("Customer.DuplicatePhoneNumber", $"A customer with phone number '{command.PhoneNumber}' already exists.");
            }
        }

        // 2. Checking for duplicate license plates in the request itself
        var requestedPlates = command.Vehicles.Select(v => v.LicensePlate.Trim().ToUpper()).ToList();
        if (requestedPlates.Count != requestedPlates.Distinct().Count())
        {
            return Error.Conflict("Vehicle.DuplicateLicensePlateInRequest", "Duplicate license plates provided in the request.");
        }

        // 3. Checking if any license plate already exists in the database
        var existingPlate = await context.Vehicles
            .Where(v => requestedPlates.Contains(v.LicensePlate.ToUpper()))
            .Select(v => v.LicensePlate)
            .FirstOrDefaultAsync(ct);

        if (existingPlate is not null)
        {
            logger.LogWarning("Customer creation aborted. License plate {LicensePlate} already exists.", existingPlate);
            return Error.Conflict("Vehicle.LicensePlateExists", $"Vehicle with license plate '{existingPlate}' already exists.");
        }

        List<Vehicle> vehicles = new();
        Guid newCustomerId = Guid.CreateVersion7();

        foreach (var v in command.Vehicles)
        {
            var vehicleResult = Vehicle.Create(Guid.CreateVersion7(), v.Make, v.Model, v.Year, v.LicensePlate, newCustomerId, timeProvider);
            if (vehicleResult.IsError) return vehicleResult.Errors;

            vehicles.Add(vehicleResult.Value);
        }

        var createCustomerResult = Customer.Create(
            id: newCustomerId,
            name: nameResult.Value,
            email: emailResult.Value,
            phoneNumber: phoneResult.Value,
            vehicles: vehicles);

        if (createCustomerResult.IsError) return createCustomerResult.Errors;

        var customer = createCustomerResult.Value;
        context.Customers.Add(customer);

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Customer created successfully. Id: {CustomerId}", customer.Id);
        return customer.ToDto();
    }
}