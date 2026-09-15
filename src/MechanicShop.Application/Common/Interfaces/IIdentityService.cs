using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string? policyName);

    Task<Result<AppUserDto>> AuthenticateAsync(string email, string password);

    Task<Result<AppUserDto>> GetUserByIdAsync(string userId);

    Task<string?> GetUserNameAsync(string userId);

    // Feature: user / onboarding management
    Task<bool> AnyUserInRoleAsync(string role);

    Task<Result<Guid>> CreateUserAsync(Guid id, string userName, string email, string password, string role, bool emailConfirmed, CancellationToken ct = default);

    Task<Result<Updated>> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct = default);

    Task<Result<string>> GeneratePasswordResetTokenByEmailAsync(string email, CancellationToken ct = default);

    Task<Result<string>> GenerateForgotPasswordTokenAsync(string email, CancellationToken ct = default);
}