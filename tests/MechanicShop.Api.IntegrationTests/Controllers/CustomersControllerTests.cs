using System.Net;
using System.Net.Http.Json;

using MechanicShop.Api.IntegrationTests.Common;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Api.Requests.Customers;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Security;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CustomersControllerTests : BaseIntegrationTest
{
    public CustomersControllerTests(WebAppFactory webAppFactory)
        : base(webAppFactory)
    {
    }

    [Fact]
    public async Task GetCustomers_WhenAuthenticated_ShouldReturnOkWithList()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        await SeedCustomerAsync(context);

        // Act
        var response = await Client.GetAsync("/api/v1.0/customers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<CustomerDto>>();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetCustomers_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        Client.ClearAuthorizationHeader();

        // Act
        var response = await Client.GetAsync("/api/v1.0/customers");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCustomerById_WithValidId_ShouldReturnCustomer()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var customer = await SeedCustomerAsync(context);

        // Act
        var response = await Client.GetAsync($"/api/v1.0/customers/{customer.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.NotNull(result);
        Assert.Equal(customer.Id, result!.CustomerId);
    }

    [Fact]
    public async Task GetCustomerById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.CreateVersion7();

        // Act
        var response = await Client.GetAsync($"/api/v1.0/customers/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_WithValidRequestAsManager_ShouldReturnCreated()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var request = new CreateCustomerRequest
        {
            FirstName = "Ahmed",
            LastName = "Ali",
            Email = "ahmed.ali@domain.com",
            PhoneNumber = "01012345678",
            Vehicles =
            [
                new CreateVehicleRequest
                {
                    Make = "Toyota",
                    Model = "Corolla",
                    Year = 2022,
                    LicensePlate = "CKA 1234"
                }
            ]
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1.0/customers", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.NotNull(result);
        Assert.Equal("Ahmed Ali", result!.Name);
        Assert.NotEmpty(result.Vehicles);
        Assert.Contains(result.CustomerId.ToString(), response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task CreateCustomer_AsLabor_ShouldReturnForbidden()
    {
        // Arrange & Policy Check (ManagerOnly)
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        var request = new CreateCustomerRequest
        {
            FirstName = "Hassan",
            LastName = "Sayed",
            Email = "hassan@domain.com",
            PhoneNumber = "01212345678",
            Vehicles = []
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1.0/customers", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCustomer_WithValidRequestAsManager_ShouldReturnNoContent()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var customer = await SeedCustomerAsync(context);
        var vehicle = customer.Vehicles.First();

        var request = new UpdateCustomerRequest
        {
            FirstName = "JohnUpdated",
            LastName = "DoeUpdated",
            Email = "updated.customer@localhost.com",
            PhoneNumber = "01198765432",
            Vehicles =
            [
                new UpdateVehicleRequest
                {
                    VehicleId = vehicle.Id,
                    Make = "BMW",
                    Model = "X5",
                    Year = 2024,
                    LicensePlate = "HIA 9999"
                }
            ]
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/v1.0/customers/{customer.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCustomer_AsLabor_ShouldReturnForbidden()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        var request = new UpdateCustomerRequest { FirstName = "Hack" };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/v1.0/customers/{Guid.CreateVersion7()}", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCustomer_WithValidIdAsManager_ShouldReturnNoContent()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var customer = await SeedCustomerAsync(context);

        // Act
        var response = await Client.DeleteAsync($"/api/v1.0/customers/{customer.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCustomer_AsLabor_ShouldReturnForbidden()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        // Act
        var response = await Client.DeleteAsync($"/api/v1.0/customers/{Guid.CreateVersion7()}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Customer> SeedCustomerAsync(IAppDbContext context)
    {
        var customer = CustomerFactory.CreateCustomer().Value;

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        return customer;
    }
}