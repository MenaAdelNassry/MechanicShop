using System;

using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Identity;

namespace MechanicShop.Tests.Common.Auth;

public static class RefreshTokenFactory
{
    public static Result<RefreshToken> CreateRefreshToken(
        Guid? id = null,
        string? token = null,
        string? userId = null,
        DateTimeOffset? expiresOnUtc = null,
        TimeProvider? timeProvider = null)
    {
        var effectiveTime = timeProvider ?? new FakeTimeProvider();
        if (timeProvider is null && effectiveTime is FakeTimeProvider fake)
        {
            fake.SetUtcNow(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        }

        return RefreshToken.Create(
            id ?? Guid.CreateVersion7(),
            token ?? "sometoken",
            userId ?? Guid.CreateVersion7().ToString(),
            expiresOnUtc ?? effectiveTime.GetUtcNow().AddDays(7),
            effectiveTime);
    }
}