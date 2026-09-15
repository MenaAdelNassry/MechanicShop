using FluentAssertions; // FluentAssertions imported

using MechanicShop.Application.Common.Behaviours;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace MechanicShop.Application.UnitTests.Behaviours;

public class CachingBehaviorTests
{
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly ILogger<CachingBehavior<CachedQuery, Result<string>>> _logger = Substitute.For<ILogger<CachingBehavior<CachedQuery, Result<string>>>>();
    private readonly CachingBehavior<CachedQuery, Result<string>> _sut;

    public CachingBehaviorTests()
    {
        _sut = new CachingBehavior<CachedQuery, Result<string>>(_cache, _logger);
    }

    [Fact]
    public async Task Handle_WhenNotCachedQuery_ShouldSkipCacheAndReturnResult()
    {
        // Arrange
        var uncachedRequest = new NonCachedQuery();
        var behavior = new CachingBehavior<NonCachedQuery, string>(_cache, Substitute.For<ILogger<CachingBehavior<NonCachedQuery, string>>>());

        // Act
        var result = await behavior.Handle(uncachedRequest, _ => Task.FromResult("OK"), CancellationToken.None);

        // Assert
        result.Should().Be("OK");

        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<HybridCacheEntryOptions>(),
            Arg.Any<string[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCachedQueryAndResultIsSuccess_ShouldCacheResult()
    {
        // Arrange
        var request = new CachedQuery();
        var response = (Result<string>)"test-value";

        // Act
        var result = await _sut.Handle(request, _ => Task.FromResult(response), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test-value");

        // Securely assert that SetAsync was invoked exactly with our context data without manual Arg.Do hooks
        await _cache.Received(1).SetAsync(
            request.CacheKey,
            response,
            Arg.Is<HybridCacheEntryOptions>(options => options.Expiration == request.Expiration),
            Arg.Is<IEnumerable<string>>(tags => tags.SequenceEqual(request.Tags)),
            CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WhenCachedQueryAndResultIsError_ShouldNotCacheResult()
    {
        // Arrange
        var request = new CachedQuery();
        var errorResult = (Result<string>)Error.Validation("code", "message");

        // Act
        var result = await _sut.Handle(request, _ => Task.FromResult(errorResult), CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();

        // Elegantly verify that no caching attempt was executed for failed Domain results
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<Result<string>>(),
            Arg.Any<HybridCacheEntryOptions>(),
            Arg.Any<string[]>(),
            Arg.Any<CancellationToken>());
    }

    public class NonCachedQuery;

    public class CachedQuery : ICachedQuery
    {
        public string CacheKey => "test-key";
        public TimeSpan Expiration => TimeSpan.FromMinutes(5);
        public string[] Tags => ["unit-test"];
    }
}