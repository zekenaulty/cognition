using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Users;
using Cognition.Testing.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Cognition.Api.Tests.Infrastructure;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ShouldProduceUniqueSaltPerInvocation()
    {
        var first = PasswordHasher.Hash("secret");
        var second = PasswordHasher.Hash("secret");

        first.Salt.Should().NotBeEquivalentTo(second.Salt);
        first.Hash.Should().NotBeEquivalentTo(second.Hash);
    }

    [Fact]
    public void Verify_ShouldReturnTrue_ForMatchingPassword()
    {
        var tuple = PasswordHasher.Hash("p@ssw0rd!");

        PasswordHasher.Verify("p@ssw0rd!", tuple.Salt, tuple.Hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_ForDifferentPassword()
    {
        var tuple = PasswordHasher.Hash("p@ssw0rd!");

        PasswordHasher.Verify("incorrect", tuple.Salt, tuple.Hash).Should().BeFalse();
    }
}

public class JwtTokenHelperTests : IDisposable
{
    private readonly string _originalSecret = JwtOptions.Secret;
    private readonly string _originalIssuer = JwtOptions.Issuer;
    private readonly string _originalAudience = JwtOptions.Audience;

    public void Dispose()
    {
        JwtOptions.Secret = _originalSecret;
        JwtOptions.Issuer = _originalIssuer;
        JwtOptions.Audience = _originalAudience;
    }

    [Fact]
    public void IssueAccessToken_UsesConfiguredSecret()
    {
        using var env = EnvironmentVariableScope.Set(new Dictionary<string, string?> { ["JWT__Secret"] = null });
        const string configuredSecret = "super-secret-key-32-bytes-long-!!!!";
        JwtOptions.Secret = configuredSecret;
        JwtOptions.Issuer = string.Empty;
        JwtOptions.Audience = string.Empty;

        var user = CreateUser();

        var (token, expiresAt) = JwtTokenHelper.IssueAccessToken(user);

        expiresAt.Should().BeAfter(DateTime.UtcNow).And.BeBefore(DateTime.UtcNow.AddHours(2).AddMinutes(1));

        var principal = ValidateToken(token, configuredSecret);
        principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value.Should().Be(user.Id.ToString());
        principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value.Should().Be(user.Username);
        principal.FindFirst("primary_persona")?.Value.Should().Be(user.PrimaryPersonaId?.ToString());
        principal.FindFirst(ClaimTypes.Role)?.Value.Should().Be(user.Role.ToString());
    }

    [Fact]
    public void IssueAccessToken_FallsBackToEnvSecret_WhenSecretUnset()
    {
        const string envSecret = "env-secret-key-32-bytes-long-!!!!!!";
        JwtOptions.Secret = string.Empty;
        JwtOptions.Issuer = string.Empty;
        JwtOptions.Audience = string.Empty;
        using var env = EnvironmentVariableScope.Set(new Dictionary<string, string?> { ["JWT__Secret"] = envSecret });

        var user = CreateUser();

        var (token, _) = JwtTokenHelper.IssueAccessToken(user);

        ValidateToken(token, envSecret).Identity.Should().NotBeNull();
    }

    [Fact]
    public void IssueAccessToken_UsesDevFallback_WhenNoSecretConfigured()
    {
        JwtOptions.Secret = string.Empty;
        JwtOptions.Issuer = string.Empty;
        JwtOptions.Audience = string.Empty;
        using var env = EnvironmentVariableScope.Set(new Dictionary<string, string?> { ["JWT__Secret"] = null });

        var user = CreateUser();

        var (token, _) = JwtTokenHelper.IssueAccessToken(user);

        ValidateToken(token, JwtOptions.DevFallbackSecret, verifySignature: false).Identity.Should().NotBeNull();
    }

    [Fact]
    public void IssueAccessToken_IncludesIssuerAndAudience_WhenConfigured()
    {
        using var env = EnvironmentVariableScope.Set(new Dictionary<string, string?> { ["JWT__Secret"] = null });
        JwtOptions.Secret = "issuer-secret-key-32-bytes-long-!!!!!";
        JwtOptions.Issuer = "https://auth.local";
        JwtOptions.Audience = "cognition-api";

        var user = CreateUser();

        var (token, _) = JwtTokenHelper.IssueAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be(JwtOptions.Issuer);
        jwt.Audiences.Should().Contain(JwtOptions.Audience);
    }

        private static ClaimsPrincipal ValidateToken(string token, string secret, bool verifySignature = true)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        if (verifySignature)
        {
            var segments = token.Split('.');
            segments.Length.Should().BeGreaterOrEqualTo(3);
            var signingInput = $"{segments[0]}.{segments[1]}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expectedSignature = Base64UrlEncoder.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput)));
            segments[2].Should().Be(expectedSignature);
        }

        var identity = new ClaimsIdentity(jwt.Claims, "jwt");
        return new ClaimsPrincipal(identity);
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = "tester",
            Role = UserRole.Administrator,
            PrimaryPersonaId = Guid.NewGuid()
        };
    }
}

public class RefreshTokenRotationTests : IDisposable
{
    private readonly string _originalSecret = JwtOptions.Secret;

    public void Dispose()
    {
        JwtOptions.Secret = _originalSecret;
    }

    [Fact]
    public async Task RotateRefreshAsync_ShouldReplaceValidHashedToken()
    {
        JwtOptions.Secret = "rotate-secret-key-32-bytes-long-!!!!!";
        await using var db = CreateContext();
        var user = CreateUser();
        db.Users.Add(user);
        var plaintext = "refresh-token";
        var hashed = Hash(plaintext);
        var refreshId = Guid.NewGuid();
        db.Set<RefreshToken>().Add(new RefreshToken
        {
            Id = refreshId,
            UserId = user.Id,
            User = user,
            Token = hashed,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        var result = await JwtTokenHelper.RotateRefreshAsync(db, plaintext);

        result.Should().NotBeNull();
        var (accessToken, expiresAt, newPlain) = result!.Value;
        accessToken.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTime.UtcNow);

        var tokens = await db.Set<RefreshToken>().ToListAsync();
        tokens.Should().HaveCount(2);
        var original = tokens.Single(t => t.Id == refreshId);
        original.RevokedAtUtc.Should().NotBeNull();
        original.Token.Should().Be(hashed);
        var replacement = tokens.Single(t => t.Id != refreshId);
        replacement.Token.Should().Be(Hash(newPlain));
    }

    [Fact]
    public async Task RotateRefreshAsync_ShouldUpgradeLegacyPlaintextToken()
    {
        JwtOptions.Secret = "rotate-secret-key-32-bytes-long-!!!!!";
        await using var db = CreateContext();
        var user = CreateUser();
        db.Users.Add(user);
        var plaintext = "legacy-token";
        var refreshId = Guid.NewGuid();
        db.Set<RefreshToken>().Add(new RefreshToken
        {
            Id = refreshId,
            UserId = user.Id,
            User = user,
            Token = plaintext,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        var result = await JwtTokenHelper.RotateRefreshAsync(db, plaintext);

        result.Should().NotBeNull();
        var tokens = await db.Set<RefreshToken>().ToListAsync();
        tokens.Should().HaveCount(2);
        tokens.Single(t => t.Id == refreshId).Token.Should().Be(Hash(plaintext));
    }

    [Fact]
    public async Task RotateRefreshAsync_ShouldReturnNull_WhenTokenExpired()
    {
        JwtOptions.Secret = "rotate-secret-key-32-bytes-long-!!!!!";
        await using var db = CreateContext();
        var user = CreateUser();
        db.Users.Add(user);
        const string plaintext = "expired";
        db.Set<RefreshToken>().Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Token = Hash(plaintext),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();

        var result = await JwtTokenHelper.RotateRefreshAsync(db, plaintext);

        result.Should().BeNull();
        (await db.Set<RefreshToken>().CountAsync()).Should().Be(1);
    }

    private static TestUserDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestUserDbContext(options);
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Username = "tester",
        Role = UserRole.User
    };

    private static string Hash(string token)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(token)));
    }

    private sealed class TestUserDbContext : CognitionDbContext
    {
        public TestUserDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var property in typeof(CognitionDbContext).GetProperties())
            {
                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                {
                    var entityType = property.PropertyType.GenericTypeArguments[0];
                    if (entityType != typeof(User) && entityType != typeof(RefreshToken))
                    {
                        modelBuilder.Ignore(entityType);
                    }
                }
            }

            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Username);
                b.Property(x => x.Role);
                b.Property(x => x.PrimaryPersonaId);
            });

            modelBuilder.Entity<RefreshToken>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Token);
                b.Property(x => x.ExpiresAtUtc);
                b.Property(x => x.RevokedAtUtc);
                b.Property(x => x.CreatedAtUtc);
                b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            });
        }
    }
}



