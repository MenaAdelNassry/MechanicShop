using FluentAssertions; // FluentAssertions imported

using MechanicShop.Application.Common.Behaviours;

using MediatR;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace MechanicShop.Application.UnitTests.Behaviours;

public class UnhandledExceptionBehaviourTests
{
    private readonly ILogger<DummyRequest> _logger = Substitute.For<ILogger<DummyRequest>>();
    private readonly UnhandledExceptionBehaviour<DummyRequest, string> _sut;

    public UnhandledExceptionBehaviourTests()
    {
        _sut = new UnhandledExceptionBehaviour<DummyRequest, string>(_logger);
    }

    [Fact]
    public async Task Handle_WhenNoException_InvokesNextAndReturnsResult()
    {
        // Arrange
        var request = new DummyRequest();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _sut.Handle(request, _ => Task.FromResult("OK"), cancellationToken);

        // Assert
        result.Should().Be("OK");
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_LogsErrorAndRethrows()
    {
        // Arrange
        var request = new DummyRequest();
        var exception = new InvalidOperationException("test failure");
        var cancellationToken = CancellationToken.None;

        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns<Task<string>>(_ => throw exception);

        // Act
        Func<Task> act = () => _sut.Handle(request, next, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(exception.Message);

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Unhandled Exception")),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    public class DummyRequest;
}