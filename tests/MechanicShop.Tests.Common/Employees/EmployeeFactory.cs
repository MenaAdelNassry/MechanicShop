using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Employees;
using MechanicShop.Domain.Identity;
using MechanicShop.Infrastructure.Identity;

namespace MechanicShop.Tests.Common.Employees;

public static class EmployeeFactory
{
    public static Result<AppUser> CreateEmployee(Guid? employeeId = null, string? firstName = null, string? lastName = null, Role? role = null, string? email = null, string? phone = null)
    {
        var userEmail = email ?? $"employee_{employeeId ?? Guid.CreateVersion7()}@shop.com";
        var userPhone = phone ?? $"0122929352{Random.Shared.Next(10)}";
        var userId = Guid.CreateVersion7();

        var employeeResult = Employee.Create(
            employeeId ?? Guid.Empty,
            PersonName.Create(firstName ?? "John", lastName ?? "Doe").Value,
            PhoneNumber.Create(userPhone).Value,
            role ?? Role.Labor,
            userId);

        if (employeeResult.IsError)
        {
            return employeeResult.TopError;
        }

        var employee = employeeResult.Value;

        var appUser = new AppUser
        {
            Id = userId,
            Email = userEmail,
            UserName = userEmail,
            EmailConfirmed = true,
            Employee = employee
        };

        return appUser;
    }

    public static Result<AppUser> CreateLabor(Guid? id = null, string? firstName = null, string? lastName = null, string? email = null)
    {
        return CreateEmployee(
            id ?? Guid.CreateVersion7(),
            firstName,
            lastName,
            Role.Labor,
            email);
    }

    public static Result<AppUser> CreateManager(Guid? id = null, string? firstName = null, string? lastName = null, string? email = null)
    {
        return CreateEmployee(
            id ?? Guid.CreateVersion7(),
            firstName ?? "John",
            lastName ?? "Manager",
            Role.Manager,
            email);
    }
}