using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Identity;

public sealed class RefreshToken : AuditableEntity
{
    public string Token { get; private set; }
    public string UserId { get; private set; }
    public DateTimeOffset ExpiresOnUtc { get; private set; }
    public DateTimeOffset? RevokedOnUtc { get; private set; }

    public bool IsExpired(TimeProvider timeProvider) => timeProvider.GetUtcNow() >= ExpiresOnUtc;
    public bool IsRevoked => RevokedOnUtc.HasValue;
    public bool IsActive(TimeProvider timeProvider) => !IsRevoked && !IsExpired(timeProvider);

#pragma warning disable CS8618
    private RefreshToken() { }
#pragma warning restore CS8618

    private RefreshToken(Guid id, string token, string userId, DateTimeOffset expiresOnUtc)
        : base(id)
    {
        Token = token;
        UserId = userId;
        ExpiresOnUtc = expiresOnUtc;
    }

    public static Result<RefreshToken> Create(
        Guid id,
        string? token,
        string? userId,
        DateTimeOffset expiresOnUtc,
        TimeProvider timeProvider)
    {
        if (id == Guid.Empty) return RefreshTokenErrors.IdRequired;
        if (string.IsNullOrWhiteSpace(token)) return RefreshTokenErrors.TokenRequired;
        if (string.IsNullOrWhiteSpace(userId)) return RefreshTokenErrors.UserIdRequired;

        if (expiresOnUtc <= timeProvider.GetUtcNow())
        {
            return RefreshTokenErrors.ExpiryInvalid;
        }

        return new RefreshToken(id, token.Trim(), userId.Trim(), expiresOnUtc);
    }

    public Result<Updated> Revoke(TimeProvider timeProvider)
    {
        if (IsRevoked)
        {
            return RefreshTokenErrors.AlreadyRevoked;
        }

        RevokedOnUtc = timeProvider.GetUtcNow();
        return Result.Updated;
    }
}