using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Cognition.Api.Tests.Infrastructure;

public class JwtTokenHelperRotateRefreshTests
{
    private const string TestSecret = "unit-test-secret-value-which-is-long-enough-123";

    [Fact]
    public async Task RotateRefreshAsync_returns_new_tokens_and_revokes_previous()
    {
        var originalSecret = JwtOptions.Secret;
        JwtOptions.Secret = TestSecret;
        try
        {
            await using var db = CreateContext();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "tester",
                NormalizedUsername = "TESTER",
                Role = UserRole.User
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var (refreshPlaintext, refreshExpires) = await JwtTokenHelper.IssueRefreshTokenAsync(db, user);
            refreshPlaintext.Should().NotBeNullOrWhiteSpace();
            refreshExpires.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));

            var firstCall = await JwtTokenHelper.RotateRefreshAsync(db, refreshPlaintext);
            firstCall.Should().NotBeNull();
            var (accessToken, accessExpiry, newRefreshPlaintext) = firstCall!.Value;
            accessToken.Should().NotBeNullOrWhiteSpace();
            accessExpiry.Should().BeAfter(DateTime.UtcNow.AddMinutes(30));
            newRefreshPlaintext.Should().NotBeNullOrWhiteSpace();

            var tokens = await db.RefreshTokens.AsNoTracking().ToListAsync();
            tokens.Should().HaveCount(2);
            var revoked = tokens.Single(t => t.Token == Hash(refreshPlaintext));
            revoked.RevokedAtUtc.Should().NotBeNull();
            var replacement = tokens.Single(t => t.Token == Hash(newRefreshPlaintext));
            replacement.RevokedAtUtc.Should().BeNull();
            replacement.ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddDays(14), TimeSpan.FromSeconds(5));
            replacement.CreatedAtUtc.Should().BeAfter(revoked.CreatedAtUtc);

            var reuseAttempt = await JwtTokenHelper.RotateRefreshAsync(db, refreshPlaintext);
            reuseAttempt.Should().BeNull();
        }
        finally
        {
            JwtOptions.Secret = originalSecret;
        }
    }

    [Fact]
    public async Task RotateRefreshAsync_returns_null_for_expired_token()
    {
        var originalSecret = JwtOptions.Secret;
        JwtOptions.Secret = TestSecret;
        try
        {
            var expiredPlain = "expired-token";
            await using var db = CreateContext();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "expired",
                NormalizedUsername = "EXPIRED",
                Role = UserRole.User
            };
            db.Users.Add(user);
            db.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = Hash(expiredPlain),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
            });
            await db.SaveChangesAsync();

            var result = await JwtTokenHelper.RotateRefreshAsync(db, expiredPlain);

            result.Should().BeNull();
            var tokens = await db.RefreshTokens.AsNoTracking().ToListAsync();
            tokens.Should().HaveCount(1);
            tokens.Single().RevokedAtUtc.Should().BeNull();
        }
        finally
        {
            JwtOptions.Secret = originalSecret;
        }
    }

    private static TestCognitionDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new TestCognitionDbContext(options);
    }

    private static string Hash(string token)
    {
        var method = typeof(JwtTokenHelper).GetMethod("HashToken", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (string)method.Invoke(null, new object[] { token })!;
    }

    private sealed class TestCognitionDbContext : CognitionDbContext
    {
        private static readonly HashSet<Type> AllowedEntities = new HashSet<Type> { typeof(User), typeof(RefreshToken) };

        public TestCognitionDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (!AllowedEntities.Contains(entityType.ClrType))
                {
                    modelBuilder.Ignore(entityType.ClrType);
                }
            }
        }
    }
}
