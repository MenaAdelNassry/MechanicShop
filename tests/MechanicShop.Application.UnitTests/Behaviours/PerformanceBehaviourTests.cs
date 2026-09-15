using FluentAssertions; // FluentAssertions imported

using MechanicShop.Application.Common.Behaviours;
using MechanicShop.Application.Common.Interfaces;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace MechanicShop.Application.UnitTests.Behaviours;

public class PerformanceBehaviourTests
{
    private readonly ILogger<TestRequest> _logger = Substitute.For<ILogger<TestRequest>>();
    private readonly IUser _user = Substitute.For<IUser>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly PerformanceBehaviour<TestRequest, TestResponse> _behaviour;

    public PerformanceBehaviourTests()
    {
        _behaviour = new PerformanceBehaviour<TestRequest, TestResponse>(
            _logger,
            _user,
            _identityService);
    }

    [Fact]
    public async Task Handle_WhenRequestTakesLessThan500Ms_ShouldNotLogWarning()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _behaviour.Handle(request, _ => Task.FromResult(expectedResponse), cancellationToken);

        // Assert
        result.Should().Be(expectedResponse);

        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenRequestTakesMoreThan500Ms_ShouldLogWarning()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var cancellationToken = CancellationToken.None;
        const string userId = "user123";
        const string userName = "TestUser";

        _user.Id.Returns(userId);
        _identityService.GetUserNameAsync(userId).Returns(userName);

        // Act
        var result = await _behaviour.Handle(request, async _ =>
        {
            await Task.Delay(600, cancellationToken); // simulate slow request
            return expectedResponse;
        }, cancellationToken);

        // Assert
        result.Should().Be(expectedResponse);

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Long Running Request") &&
                                o.ToString()!.Contains("TestRequest") &&
                                o.ToString()!.Contains(userId) &&
                                o.ToString()!.Contains(userName)),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenUserIdIsNull_ShouldLogWarningWithEmptyUserData()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var cancellationToken = CancellationToken.None;

        _user.Id.Returns((string?)null);

        // Act
        var result = await _behaviour.Handle(
            request,
            async _ =>
            {
                await Task.Delay(600, cancellationToken);
                return expectedResponse;
            },
            cancellationToken);

        // Assert
        result.Should().Be(expectedResponse);

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Long Running Request") &&
                                o.ToString()!.Contains("TestRequest")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        await _identityService.DidNotReceive().GetUserNameAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ShouldLogWarningWithEmptyUserData()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var cancellationToken = CancellationToken.None;

        _user.Id.Returns(string.Empty);

        // Act
        var result = await _behaviour.Handle(request, async _ =>
        {
            await Task.Delay(600, cancellationToken);
            return expectedResponse;
        }, cancellationToken);

        // Assert
        result.Should().Be(expectedResponse);

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Long Running Request") &&
                                o.ToString()!.Contains("TestRequest")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        await _identityService.DidNotReceive().GetUserNameAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenIdentityServiceReturnsNull_ShouldLogWarningWithNullUserName()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var cancellationToken = CancellationToken.None;
        const string userId = "user123";

        _user.Id.Returns(userId);
        _identityService.GetUserNameAsync(userId).Returns((string?)null);

        // Act
        var result = await _behaviour.Handle(request, async _ =>
        {
            await Task.Delay(600, cancellationToken); // simulate latency
            return expectedResponse;
        }, cancellationToken);

        // Assert
        result.Should().Be(expectedResponse);

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_ShouldAlwaysReturnResponseFromNext()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _behaviour.Handle(request, _ => Task.FromResult(expectedResponse), cancellationToken);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_WhenNextThrowsException_ShouldNotCatchException()
    {
        // Arrange
        var request = new TestRequest { Name = "Test" };
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        // Act
        Func<Task> act = () => _behaviour.Handle(request, _ => throw expectedException, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(expectedException.Message);
    }

    public class TestRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}