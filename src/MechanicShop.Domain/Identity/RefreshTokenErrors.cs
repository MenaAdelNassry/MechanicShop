using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Identity;

public static class RefreshTokenErrors
{
    public static readonly Error IdRequired =
        Error.Validation("RefreshToken.IdRequired", "Refresh token ID is required.");

    public static readonly Error TokenRequired =
        Error.Validation("RefreshToken.TokenRequired", "Token value is required.");

    public static readonly Error UserIdRequired =
        Error.Validation("RefreshToken.UserIdRequired", "User ID is required.");

    public static readonly Error ExpiryInvalid =
        Error.Validation("RefreshToken.ExpiryInvalid", "Expiry must be in the future.");

    public static readonly Error AlreadyRevoked =
        Error.Conflict("RefreshToken.AlreadyRevoked", "Refresh token is already revoked.");
}