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
    private static string ResolveSecret()
    {
        // Prefer the value chosen during app startup
        if (!string.IsNullOrEmpty(JwtOptions.Secret)) return JwtOptions.Secret;

        // Fallback to environment variable
        var env = Environment.GetEnvironmentVariable("JWT__Secret");
        if (!string.IsNullOrEmpty(env)) return env;

        // Final fallback for development
        return JwtOptions.DevFallbackSecret;
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
        // Hash the provided token and look up by hash first
        var hashed = HashToken(refreshToken);
        var token = await db.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.Token == hashed && t.RevokedAtUtc == null);

        // Backward-compatibility: fall back to plaintext lookup if not found (older rows)
        if (token == null)
        {
            token = await db.Set<RefreshToken>()
                .FirstOrDefaultAsync(t => t.Token == refreshToken && t.RevokedAtUtc == null);
            if (token != null)
            {
                // Upgrade stored value to hashed form
                token.Token = hashed;
                await db.SaveChangesAsync();
            }
        }
        if (token == null || token.ExpiresAtUtc < DateTime.UtcNow) return null;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId);
        if (user == null) return null;
        token.RevokedAtUtc = DateTime.UtcNow;
        var newToken = new RefreshToken
        {
            UserId = user.Id,
            // Store only the hash; return the plaintext separately
            Token = HashToken(GenerateSecureToken(out var plaintext)),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Set<RefreshToken>().Add(newToken);
        await db.SaveChangesAsync();
        var (access, exp) = IssueAccessToken(user);
        return (access, exp, plaintext);
    }

    public static async Task<(string Token, DateTime ExpiresAt)> IssueRefreshTokenAsync(CognitionDbContext db, Data.Relational.Modules.Users.User user)
    {
        var token = new RefreshToken
        {
            UserId = user.Id,
            // Store hash; return plaintext to caller
            Token = HashToken(GenerateSecureToken(out var plaintext)),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Set<RefreshToken>().Add(token);
        await db.SaveChangesAsync();
        return (plaintext, token.ExpiresAtUtc);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateSecureToken(out string plaintext)
    {
        plaintext = GenerateSecureToken();
        return plaintext;
    }

    private static string HashToken(string token)
    {
        // SHA-256 hash, returned as Base64 for compact storage
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
