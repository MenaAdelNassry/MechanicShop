using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MechanicShop.Infrastructure.Identity;

public class TokenProvider(IConfiguration configuration, IAppDbContext context, TimeProvider timeProvider) : ITokenProvider
{
    public async Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default)
    {
        var tokenResult = await CreateAsync(user, ct);

        if (tokenResult.IsError)
        {
            return tokenResult.Errors;
        }

        return tokenResult.Value;
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["JwtSettings:Audience"],
            ValidateLifetime = false, // Ignore token expiration
            ClockSkew = TimeSpan.Zero
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private async Task<Result<TokenResponse>> CreateAsync(AppUserDto user, CancellationToken ct = default)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");

        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var key = jwtSettings["Secret"]!;

        var expires = timeProvider.GetUtcNow().AddMinutes(int.Parse(jwtSettings["TokenExpirationInMinutes"]!)).UtcDateTime;

        var userIdGuid = Guid.Parse(user.UserId!);
        var employeeInfo = await context.Employees
            .Where(e => e.IdentityUserId == userIdGuid)
            .Select(e => new
            {
                e.Id,
                e.Name.FullName
            })
            .FirstOrDefaultAsync(ct);

        var employeeId = employeeInfo?.Id ?? Guid.Empty;
        var employeeName = employeeInfo?.FullName ?? user.Email ?? string.Empty;

        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, user.UserId!),
            new (JwtRegisteredClaimNames.Email, user.Email!),
            new (ClaimTypes.Name, employeeName),
            new ("EmployeeId", employeeId == Guid.Empty ? string.Empty : employeeId.ToString())
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new(ClaimTypes.Role, role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256Signature),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(descriptor);
        var accessToken = tokenHandler.WriteToken(securityToken);

        var refreshTokenResult = RefreshToken.Create(
            Guid.CreateVersion7(),
            GenerateRefreshToken(),
            user.UserId,
            timeProvider.GetUtcNow().AddDays(7),
            timeProvider);

        if (refreshTokenResult.IsError)
        {
            return refreshTokenResult.Errors;
        }

        var refreshToken = refreshTokenResult.Value;

        context.RefreshTokens.Add(refreshToken);

        await context.SaveChangesAsync(ct);

        return new TokenResponse(accessToken, refreshToken.Token, expires);
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}