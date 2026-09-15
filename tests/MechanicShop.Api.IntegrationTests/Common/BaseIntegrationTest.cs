using MechanicShop.Domain.Identity;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Api.IntegrationTests.Common;

// Collection fixture only — IClassFixture would create a second SQL container.
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly WebAppFactory Factory;
    protected readonly AppHttpClient Client;

    protected BaseIntegrationTest(WebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateAppHttpClient();
    }

    public async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();

        await SeedTestUsersAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedTestUsersAsync()
    {
        using var scope = Factory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // a) First, verify that the basic roles are present
        var roles = new[] { nameof(Role.Manager), nameof(Role.Labor), nameof(Role.InventoryManager) };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        // b) Add Manager
        var managerUser = TestUsers.Manager;
        if (await userManager.FindByEmailAsync(managerUser.Email!) is null)
        {
            await userManager.CreateAsync(managerUser, "Password123!");
            await userManager.AddToRoleAsync(managerUser, nameof(Role.Manager));
        }

        // c) Add Labors
        var labors = new[] { TestUsers.Labor01, TestUsers.Labor02 };
        foreach (var laborUser in labors)
        {
            if (await userManager.FindByEmailAsync(laborUser.Email!) is null)
            {
                await userManager.CreateAsync(laborUser, "Password123!");
                await userManager.AddToRoleAsync(laborUser, nameof(Role.Labor));
            }
        }
    }
}