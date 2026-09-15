namespace MechanicShop.Api.Requests.Identity;

public sealed record SetPasswordRequest(string Email, string Token, string NewPassword);