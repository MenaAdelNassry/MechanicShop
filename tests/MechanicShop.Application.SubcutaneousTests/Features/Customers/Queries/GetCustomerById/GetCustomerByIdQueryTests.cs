using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Customers.Queries.GetCustomerById;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Tests.Common.Customers;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Customers.Queries.GetCustomerById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetCustomerByIdQueryTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnCustomerWithVehicles()
    {
        // Arrange
        var customer = CustomerFactory.CreateCustomer().Value;

        using (var arrangeScope = _factory.CreateScope())
        {
            var context = arrangeScope.ServiceProvider.GetRequiredService<IAppDbContext>();
            await context.Customers.AddAsync(customer);
            await context.SaveChangesAsync(default);
        }

        var query = new GetCustomerByIdQuery(customer.Id);

        // Act
        using var actScope = _factory.CreateScope();
        var mediator = actScope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.CustomerId.Should().Be(customer.Id);
        result.Value.Vehicles.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenCustomerDoesNotExist_ShouldReturnNotFoundError()
    {
        // 1️⃣ Arrange
        var nonExistentCustomerId = Guid.CreateVersion7();
        var query = new GetCustomerByIdQuery(nonExistentCustomerId);

        // 2️⃣ Act
        using var actScope = _factory.CreateScope();
        var mediator = actScope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(query);

        // 3️⃣ Assert
        result.IsSuccess.Should().BeFalse();
        result.TopError.Type.Should().Be(ErrorKind.NotFound);
        result.TopError.Code.Should().Be("Customer_NotFound");
    }
}