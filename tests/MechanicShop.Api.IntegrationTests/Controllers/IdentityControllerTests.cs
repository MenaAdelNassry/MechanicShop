using System.Net;
using System.Net.Http.Json;

using MechanicShop.Api.IntegrationTests.Common;
using MechanicShop.Application.Features.Identity;
using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Application.Features.Identity.Queries.GenerateTokens;
using MechanicShop.Application.Features.Identity.Queries.RefreshTokens;
using MechanicShop.Tests.Common.Security;

using Xunit;

namespace MechanicShop.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class IdentityControllerTests : BaseIntegrationTest
{
    public IdentityControllerTests(WebAppFactory webAppFactory)
        : base(webAppFactory)
    {
    }

    [Fact]
    public async Task GenerateToken_WithValidCredentials_ShouldReturnOkWithTokens()
    {
        // Arrange
        var query = new GenerateTokenQuery(TestUsers.Manager.Email!, "Password123!");

        // Act
        var response = await Client.PostAsJsonAsync("/identity/token/generate", query);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        Assert.NotNull(tokens!.AccessToken);
        Assert.NotNull(tokens.RefreshToken);
        Assert.True(tokens.ExpiresOnUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateToken_WithInvalidCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        var query = new GenerateTokenQuery(TestUsers.Manager.Email!, "WrongPassword!");

        // Act
        var response = await Client.PostAsJsonAsync("/identity/token/generate", query);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidTokens_ShouldReturnNewTokens()
    {
        // Arrange
        var loginQuery = new GenerateTokenQuery(TestUsers.Manager.Email!, "Password123!");
        var loginResponse = await Client.PostAsJsonAsync("/identity/token/generate", loginQuery);
        var initialTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();

        // Generating the RefreshToken request
        var refreshQuery = new RefreshTokenQuery(initialTokens!.RefreshToken!, initialTokens.AccessToken!);

        // Act
        var response = await Client.PostAsJsonAsync("/identity/token/refresh-token", refreshQuery);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var newTokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(newTokens);
        Assert.NotNull(newTokens!.AccessToken);
        Assert.NotNull(newTokens.RefreshToken);
    }

    [Fact]
    public async Task GetCurrentUserInfo_WhenAuthenticated_ShouldReturnOkWithUserInfo()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        // Act
        var response = await Client.GetAsync("/identity/current-user/claims");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var userInfo = await response.Content.ReadFromJsonAsync<AppUserDto>();
        Assert.NotNull(userInfo);
        Assert.Equal(TestUsers.Manager.Email, userInfo!.Email);
    }

    [Fact]
    public async Task GetCurrentUserInfo_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        Client.ClearAuthorizationHeader();

        // Act
        var response = await Client.GetAsync("/identity/current-user/claims");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}