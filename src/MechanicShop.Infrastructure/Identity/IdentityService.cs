using System.Text;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Domain.Common.Results;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Infrastructure.Identity;

public class IdentityService(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
    IAuthorizationService authorizationService,
    IAppDbContext context,
    ILogger<IdentityService> logger,
    TimeProvider timeProvider) : IIdentityService
{
    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user != null && await userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string? policyName)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var principal = await userClaimsPrincipalFactory.CreateAsync(user);
        var result = await authorizationService.AuthorizeAsync(principal, policyName!);

        return result.Succeeded;
    }

    public async Task<Result<AppUserDto>> AuthenticateAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            return Error.Conflict("Invalid_Credentials", "Invalid email or password.");
        }

        if (!user.EmailConfirmed)
            return Error.Conflict("Email_Not_Confirmed", $"Email '{UtilityService.MaskEmail(email)}' is not confirmed");

        return await BuildUserDtoAsync(user);
    }

    public async Task<Result<AppUserDto>> GetUserByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Error.NotFound("Identity.UserNotFound", $"User with Id '{userId}' was not found.");

        return await BuildUserDtoAsync(user);
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user?.UserName;
    }

    public async Task<bool> AnyUserInRoleAsync(string role)
    {
        var usersInRole = await userManager.GetUsersInRoleAsync(role);
        return usersInRole is { Count: > 0 };
    }

    public async Task<Result<Guid>> CreateUserAsync(Guid id, string userName, string email, string password, string role, bool emailConfirmed, CancellationToken ct = default)
    {
        var exists = await userManager.FindByEmailAsync(email);
        if (exists is not null)
            return Error.Conflict("Identity.UserExists", "A user with the specified email already exists.");

        if (!string.IsNullOrWhiteSpace(role) && !await roleManager.RoleExistsAsync(role))
            return Error.NotFound("Identity.RoleNotFound", $"Role '{role}' does not exist in the system.");

        var appUser = new AppUser
        {
            Id = id,
            Email = email,
            UserName = userName,
            EmailConfirmed = emailConfirmed,
        };

        var createResult = await userManager.CreateAsync(appUser, password);
        if (!createResult.Succeeded)
            return FormatIdentityErrors("Identity.CreateUserFailed", createResult.Errors);

        if (!string.IsNullOrWhiteSpace(role))
        {
            var addRoleResult = await userManager.AddToRoleAsync(appUser, role);
            if (!addRoleResult.Succeeded)
                return FormatIdentityErrors("Identity.AddRoleFailed", addRoleResult.Errors);
        }

        return appUser.Id;
    }

    public async Task<Result<string>> GeneratePasswordResetTokenByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Error.NotFound("Identity.UserNotFound", "User not found.");

        if (user.EmailConfirmed)
            return Error.Conflict("Identity.AlreadyConfirmed", "Account is already set up and confirmed.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        logger.LogInformation("Generated raw token for user {Email}: {Token}", email, token);

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        logger.LogInformation("Generated Base64Url encoded token for user {Email}: {Token}", email, encodedToken);

        return encodedToken;
    }

    public async Task<Result<string>> GenerateForgotPasswordTokenAsync(string email, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Error.NotFound("Identity.UserNotFound", "User not found.");

        if (!user.EmailConfirmed)
            return Error.Validation("Identity.EmailNotConfirmed", "Account is not active or email not confirmed.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        logger.LogInformation("Generated forgot-password raw token for user {Email}: {Token}", email, token);

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        return encodedToken;
    }

    public async Task<Result<Updated>> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Error.NotFound("Identity.UserNotFound", "User not found.");

        string decodedToken = DecodeToken(token, logger);

        var result = await userManager.ResetPasswordAsync(user, decodedToken, newPassword);
        if (!result.Succeeded)
            return FormatIdentityErrors("Identity.InvalidTokenOrPassword", result.Errors);

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        var now = timeProvider.GetUtcNow();
        var userIdStr = user.Id.ToString();

        await context.RefreshTokens
            .Where(rt => rt.UserId == userIdStr && rt.RevokedOnUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.RevokedOnUtc, now), ct);

        return Result.Updated;
    }

    private async Task<AppUserDto> BuildUserDtoAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        return new AppUserDto(user.Id.ToString(), user.Email!, roles);
    }

    private static Error FormatIdentityErrors(string code, IEnumerable<IdentityError> errors)
    {
        var desc = string.Join(" ; ", errors.Select(e => e.Description));
        return Error.Validation(code, desc);
    }

    private static string DecodeToken(string token, ILogger<IdentityService> logger)
    {
        try
        {
            logger.LogInformation("The received token: {Token}", token);
            var decodedBytes = WebEncoders.Base64UrlDecode(token);
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            logger.LogInformation("The decoded token: {DecodedToken}", decodedString);
            return decodedString;
        }
        catch
        {
            logger.LogInformation("The received token is not Base64Url encoded: {Token}", token);
            return token;
        }
    }
}