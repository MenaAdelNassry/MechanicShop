using System.Security.Claims;

using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity;
using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Application.Features.Identity.Queries.GenerateTokens;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Common.Results;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Identity.Queries.GenerateTokens;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GenerateTokenQueryTests : BaseSubcutaneousTest
{
    private readonly IIdentityService _mockIdentityService;
    private readonly ITokenProvider _mockTokenProvider;
    private readonly ILogger<GenerateTokenQueryHandler> _mockLogger;
    private readonly GenerateTokenQueryHandler _handler;

    public GenerateTokenQueryTests(WebAppFactory factory)
        : base(factory)
    {
        _mockIdentityService = Substitute.For<IIdentityService>();
        _mockTokenProvider = Substitute.For<ITokenProvider>();
        _mockLogger = Substitute.For<ILogger<GenerateTokenQueryHandler>>();

        _handler = new GenerateTokenQueryHandler(
            _mockLogger,
            _mockIdentityService,
            _mockTokenProvider);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokenResponse()
    {
        // Arrange
        var email = "mechanic@shop.com";
        var password = "CorrectPassword123";
        var query = new GenerateTokenQuery(email, password);

        // Setting up a valid AppUserDto instance using its correct constructor signature
        var mockUserDto = new AppUserDto(
            UserId: Guid.CreateVersion7().ToString(),
            Email: email,
            Roles: new List<string> { "Labor" },
            Claims: new List<Claim>());

        // Using implicit conversion: Task<Result<AppUserDto>> accepts mockUserDto directly
        _mockIdentityService.AuthenticateAsync(email, password)
            .Returns(Task.FromResult<Result<AppUserDto>>(mockUserDto));

        // Initializing the correct TokenResponse object properties
        var expectedTokenResponse = new TokenResponse
        {
            AccessToken = "AccessTokenValue",
            RefreshToken = "RefreshTokenValue",
            ExpiresOnUtc = DateTime.UtcNow.AddHours(1)
        };

        // Using implicit conversion: Task<Result<TokenResponse>> accepts expectedTokenResponse directly
        _mockTokenProvider.GenerateJwtTokenAsync(mockUserDto, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<TokenResponse>>(expectedTokenResponse));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.AccessToken.Should().Be("AccessTokenValue");
        result.Value.RefreshToken.Should().Be("RefreshTokenValue");
    }

    [Fact]
    public async Task Handle_WhenAuthenticationFails_ShouldReturnAuthenticationError()
    {
        // Arrange
        var email = "wrong@shop.com";
        var password = "WrongPassword";
        var query = new GenerateTokenQuery(email, password);

        var authError = Error.Validation("Identity_InvalidCredentials", "Invalid username or password.");

        // Using implicit conversion to mock the failure result directly
        _mockIdentityService.AuthenticateAsync(email, password)
            .Returns(Task.FromResult<Result<AppUserDto>>(authError));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Identity_InvalidCredentials");

        // Verify token provider was never called since authentication failed
        await _mockTokenProvider.DidNotReceive()
            .GenerateJwtTokenAsync(Arg.Any<AppUserDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenGenerationFails_ShouldReturnErrorAndLogDetails()
    {
        // Arrange
        var email = "mechanic@shop.com";
        var password = "CorrectPassword123";
        var query = new GenerateTokenQuery(email, password);

        var mockUserDto = new AppUserDto(
            UserId: Guid.CreateVersion7().ToString(),
            Email: email,
            Roles: new List<string> { "Labor" },
            Claims: new List<Claim>());

        _mockIdentityService.AuthenticateAsync(email, password)
            .Returns(Task.FromResult<Result<AppUserDto>>(mockUserDto));

        var tokenError = Error.Failure("Token_GenerationFailed", "Failed to compile security algorithms.");

        _mockTokenProvider.GenerateJwtTokenAsync(mockUserDto, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<TokenResponse>>(tokenError));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Token_GenerationFailed");
    }
}