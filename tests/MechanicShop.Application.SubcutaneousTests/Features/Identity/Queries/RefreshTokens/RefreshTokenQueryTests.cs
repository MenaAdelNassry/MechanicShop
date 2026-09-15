using System.Security.Claims;

using FluentAssertions;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity;
using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Application.Features.Identity.Queries.RefreshTokens;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Identity.Queries.RefreshTokens;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RefreshTokenQueryTests : BaseSubcutaneousTest
{
    private readonly WebAppFactory _factory;
    private readonly IIdentityService _mockIdentityService;
    private readonly ITokenProvider _mockTokenProvider;
    private readonly ILogger<RefreshTokenQueryHandler> _mockLogger;

    public RefreshTokenQueryTests(WebAppFactory factory)
        : base(factory)
    {
        _factory = factory;
        _mockIdentityService = Substitute.For<IIdentityService>();
        _mockTokenProvider = Substitute.For<ITokenProvider>();
        _mockLogger = Substitute.For<ILogger<RefreshTokenQueryHandler>>();
    }

    [Fact]
    public async Task Handle_WithValidTokens_ShouldReturnNewTokenResponse()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var expiredAccessToken = "expired-token";
        var refreshTokenStr = "valid-refresh-token-" + Guid.CreateVersion7();
        var userId = Guid.CreateVersion7().ToString();

        var query = new RefreshTokenQuery(refreshTokenStr, expiredAccessToken);

        // 1. Mock the claims principal decoding
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _mockTokenProvider.GetPrincipalFromExpiredToken(expiredAccessToken).Returns(principal);

        // 2. Mock Identity Service response
        var mockUserDto = new AppUserDto(userId, "user@shop.com", new List<string> { "Labor" }, new List<Claim>());
        _mockIdentityService.GetUserByIdAsync(userId).Returns(Task.FromResult<Result<AppUserDto>>(mockUserDto));

        // 3. Create and Save actual RefreshToken using the domain's Create factory method
        var expiresOnUtc = DateTimeOffset.UtcNow.AddDays(1);
        var refreshToken = RefreshToken.Create(Guid.CreateVersion7(), refreshTokenStr, userId, expiresOnUtc, timeProvider).Value;

        await context.RefreshTokens.AddAsync(refreshToken);
        await context.SaveChangesAsync(default);

        // 4. Mock Token Provider generating the new tokens
        var expectedTokenResponse = new TokenResponse
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            ExpiresOnUtc = DateTime.UtcNow.AddHours(1)
        };
        _mockTokenProvider.GenerateJwtTokenAsync(mockUserDto, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<TokenResponse>>(expectedTokenResponse));

        var handler = new RefreshTokenQueryHandler(_mockLogger, _mockIdentityService, context, _mockTokenProvider);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task Handle_WhenExpiredAccessTokenIsInvalid_ShouldReturnExpiredAccessTokenInvalidError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var query = new RefreshTokenQuery("some-refresh", "invalid-access-token");
        _mockTokenProvider.GetPrincipalFromExpiredToken(Arg.Any<string>()).Returns((ClaimsPrincipal)null!);

        var handler = new RefreshTokenQueryHandler(_mockLogger, _mockIdentityService, context, _mockTokenProvider);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Auth.ExpiredAccessToken.Invalid");
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenDoesNotExistInDb_ShouldReturnRefreshTokenExpiredError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var expiredAccessToken = "expired-token";
        var query = new RefreshTokenQuery("non-existent-refresh", expiredAccessToken);
        var userId = Guid.CreateVersion7().ToString();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        _mockTokenProvider.GetPrincipalFromExpiredToken(expiredAccessToken).Returns(principal);

        var mockUserDto = new AppUserDto(userId, "user@shop.com", new List<string> { "Labor" }, new List<Claim>());
        _mockIdentityService.GetUserByIdAsync(userId).Returns(Task.FromResult<Result<AppUserDto>>(mockUserDto));

        var handler = new RefreshTokenQueryHandler(_mockLogger, _mockIdentityService, context, _mockTokenProvider);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Auth.RefreshToken.Expired");
    }

    [Fact]
    public async Task Handle_WhenUserAttemptsToUseAnotherUsersRefreshToken_ShouldReturnSecurityViolationError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var expiredAccessToken = "expired-token";
        var query = new RefreshTokenQuery("attacker-refresh-token", expiredAccessToken);

        var realUserId = Guid.CreateVersion7().ToString();
        var victimUserId = Guid.CreateVersion7().ToString();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, realUserId) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        _mockTokenProvider.GetPrincipalFromExpiredToken(expiredAccessToken).Returns(principal);

        var mockUserDto = new AppUserDto(realUserId, "user@shop.com", new List<string> { "Labor" }, new List<Claim>());
        _mockIdentityService.GetUserByIdAsync(realUserId).Returns(Task.FromResult<Result<AppUserDto>>(mockUserDto));

        // Save refresh token belonging to the victim
        var expiresOnUtc = DateTimeOffset.UtcNow.AddDays(1);
        var victimRefreshToken = RefreshToken.Create(Guid.CreateVersion7(), "attacker-refresh-token", victimUserId, expiresOnUtc, timeProvider).Value;

        await context.RefreshTokens.AddAsync(victimRefreshToken);
        await context.SaveChangesAsync(default);

        var handler = new RefreshTokenQueryHandler(_mockLogger, _mockIdentityService, context, _mockTokenProvider);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Auth.RefreshToken.Expired");
    }
}