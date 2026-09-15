using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Customers.Queries.GetCustomers;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Tests.Common.Customers;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Customers.Queries.GetCustomers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetCustomersQueryTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WhenCustomersExist_ShouldReturnListWithAllCustomersAndTheirVehicles()
    {
        var customerId1 = Guid.Parse("bf4bbf1b-a117-4033-b13d-84edaec2203b");
        var customerId2 = Guid.Parse("bf4bbf1b-a117-4033-b13d-84edaec3303d");

        var customer1 = CustomerFactory.CreateCustomer(id: customerId1).Value;
        var customer2 = CustomerFactory.CreateCustomer(id: customerId2).Value;

        using (var arrangeScope = _factory.CreateScope())
        {
            var context = arrangeScope.ServiceProvider.GetRequiredService<IAppDbContext>();
            await context.Customers.AddRangeAsync(customer1, customer2);
            await context.SaveChangesAsync(default);
        }

        var query = new GetCustomersQuery();

        using var actScope = _factory.CreateScope();
        var mediator = actScope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        result.Value.Should().HaveCount(2);
        result.Value.Any(c => c.CustomerId == customer1.Id).Should().BeTrue();
        result.Value.Any(c => c.CustomerId == customer2.Id).Should().BeTrue();

        result.Value.ForEach(c => c.Vehicles.Should().NotBeNull());
    }

    [Fact]
    public async Task Handle_WhenNoCustomersExist_ShouldReturnEmptyList()
    {
        var query = new GetCustomersQuery();

        using var actScope = _factory.CreateScope();
        var mediator = actScope.ServiceProvider.GetRequiredService<IMediator>();
        var dbContext = actScope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var result = await mediator.Send(query);

        var customersInDb = await dbContext.Customers.ToListAsync();

        customersInDb.Should().BeEmpty();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }
}