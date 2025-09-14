using System.Security.Cryptography;

namespace Cognition.Api.Infrastructure;

public static class PasswordHasher
{
    public static (byte[] Hash, byte[] Salt, string Algo, int Version) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return (hash, salt, "pbkdf2", 1);
    }

    public static bool Verify(string password, byte[] salt, byte[] expected)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(hash, expected);
    }
}

