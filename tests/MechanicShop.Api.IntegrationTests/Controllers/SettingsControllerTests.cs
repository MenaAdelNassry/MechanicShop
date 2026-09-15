using System.Net;
using System.Net.Http.Json;

using MechanicShop.Api.IntegrationTests.Common;
using MechanicShop.Application.Common.Settings;
using MechanicShop.Api.Responses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MechanicShop.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class SettingsControllerTests : BaseIntegrationTest
{
    public SettingsControllerTests(WebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetOperatingHours_ShouldReturnConfiguredTimes()
    {
        // Arrange - read expected values from the host configuration
        using var scope = Factory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>();
        var expected = options.Value;

        // Act
        var response = await Client.GetAsync("/api/settings/operating-hours");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperatingHoursResponse>();
        Assert.NotNull(result);
        Assert.Equal(expected.OpeningTime, result!.OpeningTime);
        Assert.Equal(expected.ClosingTime, result.ClosingTime);
    }

    [Fact]
    public async Task GetOperatingHours_WithoutAuthentication_ShouldReturnOk()
    {
        // Arrange
        Client.ClearAuthorizationHeader();

        // Act
        var response = await Client.GetAsync("/api/settings/operating-hours");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}