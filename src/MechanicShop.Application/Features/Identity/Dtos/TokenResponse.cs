namespace MechanicShop.Application.Features.Identity.Dtos;

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresOnUtc);