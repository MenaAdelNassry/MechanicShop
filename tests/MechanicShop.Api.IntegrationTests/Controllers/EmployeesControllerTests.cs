using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using MechanicShop.Api.IntegrationTests.Common;
using MechanicShop.Application.Features.Employees.Dtos;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class EmployeesControllerTests : BaseIntegrationTest
{
    public EmployeesControllerTests(WebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Get_AsManagerFilteredByLabor_ShouldReturnListOfLabors()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        // Create test users with Domain Employee instances attached
        var appUser1 = EmployeeFactory.CreateLabor(firstName: "Alice", lastName: "Mechanic").Value;
        var appUser2 = EmployeeFactory.CreateLabor(firstName: "Bob", lastName: "Mechanic").Value;

        var employee1 = appUser1.Employee!;
        var employee2 = appUser2.Employee!;

        using (var scope = Factory.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            // Create first user and seed their Identity Role explicitly
            var identityResult1 = await userManager.CreateAsync(appUser1, "SecurePassword123!");
            identityResult1.Succeeded.Should().BeTrue();

            var roleResult1 = await userManager.AddToRoleAsync(appUser1, "Labor");
            roleResult1.Succeeded.Should().BeTrue();

            // Create second user and seed their Identity Role explicitly
            var identityResult2 = await userManager.CreateAsync(appUser2, "SecurePassword123!");
            identityResult2.Succeeded.Should().BeTrue();

            var roleResult2 = await userManager.AddToRoleAsync(appUser2, "Labor");
            roleResult2.Succeeded.Should().BeTrue();
        }

        // Act - Call the updated and merged route with a role filter parameter
        var response = await Client.GetAsync("/api/v1.0/employees?role=Labor");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dtos = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>();
        Assert.NotNull(dtos);

        // Verify each inserted employee is present with correct metadata mapping
        Assert.Contains(dtos, d => d.EmployeeId == employee1.Id && d.Name.Contains("Alice") && d.Role == "Labor");
        Assert.Contains(dtos, d => d.EmployeeId == employee2.Id && d.Name.Contains("Bob") && d.Role == "Labor");
    }

    [Fact]
    public async Task Get_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        Client.ClearAuthorizationHeader();

        // Act - Call the updated route anonymously
        var response = await Client.GetAsync("/api/v1.0/employees?role=Labor");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}