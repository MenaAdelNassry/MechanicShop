using FluentAssertions;

using MechanicShop.Domain.Identity;
using MechanicShop.Tests.Common;
using MechanicShop.Tests.Common.Auth;

using Xunit;

namespace MechanicShop.Domain.UnitTests.Auth;

public class RefreshTokenTests
{
    [Fact]
    public void CreateRefreshToken_ShouldSucceed_WithValidData()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7().ToString();
        const string tokenValue = "token";

        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var expiresOnUtc = fakeTime.GetUtcNow().AddDays(7);

        // Act
        var result = RefreshTokenFactory.CreateRefreshToken(
            id: id,
            token: tokenValue,
            userId: userId,
            expiresOnUtc: expiresOnUtc,
            timeProvider: fakeTime);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var token = result.Value;
        token.Should().NotBeNull();
        token.Token.Should().Be(tokenValue);
        token.UserId.Should().Be(userId);
        token.ExpiresOnUtc.Should().Be(expiresOnUtc);
    }

    [Fact]
    public void CreateRefreshToken_ShouldFail_WhenIdEmpty()
    {
        // Act
        var result = RefreshTokenFactory.CreateRefreshToken(id: Guid.Empty);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(RefreshTokenErrors.IdRequired.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRefreshToken_ShouldFail_WhenTokenInvalid(string? invalidToken)
    {
        // Act
        var result = RefreshTokenFactory.CreateRefreshToken(token: invalidToken);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(RefreshTokenErrors.TokenRequired.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRefreshToken_ShouldFail_WhenUserIdInvalid(string? invalidUserId)
    {
        // Act
        var result = RefreshTokenFactory.CreateRefreshToken(userId: invalidUserId);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(RefreshTokenErrors.UserIdRequired.Code);
    }

    [Fact]
    public void CreateRefreshToken_ShouldFail_WhenExpiresOnUtcIsInPast()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        // Setting expiry 1 minute behind the controlled frozen timeline
        var pastExpiry = fakeTime.GetUtcNow().AddMinutes(-1);

        // Act
        var result = RefreshTokenFactory.CreateRefreshToken(expiresOnUtc: pastExpiry, timeProvider: fakeTime);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(RefreshTokenErrors.ExpiryInvalid.Code);
    }
}