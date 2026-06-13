using System.Security.Cryptography;
using FurkanTural_Application.Services.Abstract;

namespace FurkanTural_Business.Services.Concrete;

/// <summary>
/// PBKDF2-SHA256 tabanlı parola özetleyici. Format kendini tanımlar:
/// <c>PBKDF2$&lt;iterasyon&gt;$&lt;saltBase64&gt;$&lt;hashBase64&gt;</c> — iterasyon sayısı
/// ileride artırılırsa eski kayıtlar kendi iterasyonuyla doğrulanmaya devam eder.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const string Prefix = "PBKDF2";
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string stored)
    {
        if (string.IsNullOrWhiteSpace(password) || !IsHashed(stored))
            return false;

        var parts = stored.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
            return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public bool IsHashed(string? stored)
        => stored is not null && stored.StartsWith(Prefix + "$", StringComparison.Ordinal);
}
