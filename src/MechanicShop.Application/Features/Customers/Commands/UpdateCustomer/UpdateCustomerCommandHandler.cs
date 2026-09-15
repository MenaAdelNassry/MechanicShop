using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Customers.Vehicles;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommandHandler(
    ILogger<UpdateCustomerCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider
    )
    : IRequestHandler<UpdateCustomerCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(UpdateCustomerCommand command, CancellationToken ct)
    {
        var customer = await context.Customers
             .Include(rt => rt.Vehicles)
             .FirstOrDefaultAsync(rt => rt.Id == command.CustomerId, ct);

        if (customer is null)
        {
            logger.LogWarning("Customer {CustomerId} not found for update.", command.CustomerId);
            return ApplicationErrors.Customers.NotFound;
        }

        var incomingIds = command.Vehicles
            .Select(v => v.VehicleId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        var removedIds = customer.Vehicles
            .Select(v => v.Id)
            .Except(incomingIds)
            .ToList();

        if (removedIds.Any())
        {
            var hasActiveOrders = await context.WorkOrders
                .AnyAsync(wo => removedIds.Contains(wo.VehicleId), ct);

            if (hasActiveOrders)
            {
                logger.LogWarning("Update aborted. One or more vehicles are in use by work orders.");
                return ApplicationErrors.Customers.VehicleInUse;
            }
        }

        // 1. Checking for duplicate license plates in the request itself
        var requestedPlates = command.Vehicles.Select(v => v.LicensePlate.Trim().ToUpper()).ToList();
        if (requestedPlates.Count != requestedPlates.Distinct().Count())
        {
            return Error.Conflict("Vehicle.DuplicateLicensePlateInRequest", "Duplicate license plates provided in the request.");
        }

        // 2. Checking if any license plate already exists in the database
        var duplicatePlate = await context.Vehicles
            .Where(v => !incomingIds.Contains(v.Id) && requestedPlates.Contains(v.LicensePlate.ToUpper()))
            .Select(v => v.LicensePlate)
            .FirstOrDefaultAsync(ct);

        if (duplicatePlate is not null)
        {
            logger.LogWarning("Update aborted. License plate {LicensePlate} is already assigned to another customer.", duplicatePlate);
            return Error.Conflict("Vehicle.LicensePlateExists", $"Vehicle with license plate '{duplicatePlate}' belongs to another customer.");
        }

        var validatedVehicles = command.Vehicles
            .Select(v => new VehicleData(v.VehicleId, v.Make, v.Model, v.Year, v.LicensePlate))
            .ToList();

        var nameResult = PersonName.Create(command.FirstName, command.LastName);
        if (nameResult.IsError) return nameResult.Errors;

        var phoneResult = PhoneNumber.Create(command.PhoneNumber);
        if (phoneResult.IsError) return phoneResult.Errors;

        var emailResult = EmailAddress.Create(command.Email);
        if (emailResult.IsError) return emailResult.Errors;

        var emailExistsForOther = await context.Customers
            .AnyAsync(c => c.Id != command.CustomerId && c.Email == emailResult.Value, ct);

        if (emailExistsForOther)
        {
            logger.LogWarning("Update aborted. Email {Email} is already taken by another customer.", command.Email);
            return CustomerErrors.EmailAlreadyExists;
        }

        var updateCustomerResult = customer.Update(nameResult.Value, emailResult.Value, phoneResult.Value);
        if (updateCustomerResult.IsError) return updateCustomerResult.Errors;

        var upsertPartsResult = customer.UpsertVehicles(validatedVehicles, timeProvider);
        if (upsertPartsResult.IsError) return upsertPartsResult.Errors;

        await context.SaveChangesAsync(ct);

        return Result.Updated;
    }
}