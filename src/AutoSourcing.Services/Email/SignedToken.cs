using System.Security.Cryptography;
using System.Text;

namespace AutoSourcing.Services.Email;

// Short, tamper-proof tokens for links in outbound email (so ids can't be guessed).
public static class SignedToken
{
    public static string Create(string purpose, string value, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{purpose}:{value}"));
        return Convert.ToBase64String(hash.AsSpan(0, 16))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool Verify(string purpose, string value, string? token, byte[] key)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var expected = Create(purpose, value, key);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(token));
    }
}
