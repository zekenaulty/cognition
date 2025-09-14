using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Cognition.Api.Infrastructure;

public static class JwtTokenHelper
{
    private const string DevFallbackSecret = "dev-secret-change-me-please-change-32bytes!dev"; // >= 32 bytes

    private static string ResolveSecret()
    {
        // Prefer the value chosen during app startup
        if (!string.IsNullOrEmpty(JwtOptions.Secret)) return JwtOptions.Secret;

        // Fallback to environment variable
        var env = Environment.GetEnvironmentVariable("JWT__Secret");
        if (!string.IsNullOrEmpty(env)) return env;

        // Final fallback for development
        return DevFallbackSecret;
    }

    public static (string AccessToken, DateTime ExpiresAt) IssueAccessToken(Data.Relational.Modules.Users.User user)
    {
        var secret = ResolveSecret();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(2);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("primary_persona", user.PrimaryPersonaId?.ToString() ?? string.Empty),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var token = new JwtSecurityToken(
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwt, expires);
    }

    public static async Task<(string AccessToken, DateTime ExpiresAt, string RefreshToken)?> RotateRefreshAsync(CognitionDbContext db, string refreshToken)
    {
        var token = await db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.Token == refreshToken && t.RevokedAtUtc == null);
        if (token == null || token.ExpiresAtUtc < DateTime.UtcNow) return null;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId);
        if (user == null) return null;
        token.RevokedAtUtc = DateTime.UtcNow;
        var newToken = new RefreshToken
        {
            UserId = user.Id,
            Token = GenerateSecureToken(),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Set<RefreshToken>().Add(newToken);
        await db.SaveChangesAsync();
        var (access, exp) = IssueAccessToken(user);
        return (access, exp, newToken.Token);
    }

    public static async Task<(string Token, DateTime ExpiresAt)> IssueRefreshTokenAsync(CognitionDbContext db, Data.Relational.Modules.Users.User user)
    {
        var token = new RefreshToken
        {
            UserId = user.Id,
            Token = GenerateSecureToken(),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Set<RefreshToken>().Add(token);
        await db.SaveChangesAsync();
        return (token.Token, token.ExpiresAtUtc);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
