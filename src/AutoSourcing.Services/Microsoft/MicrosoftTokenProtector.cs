using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace AutoSourcing.Services.Microsoft;

// Encrypts sensitive values (Microsoft refresh tokens) at rest in the database.
public interface ITokenProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}

public class AesTokenProtector : ITokenProtector
{
    private readonly MicrosoftOptions _options;

    public AesTokenProtector(IOptions<MicrosoftOptions> options) => _options = options.Value;

    private byte[] Key
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                throw new InvalidOperationException("Microsoft:ClientSecret is required to protect stored tokens.");
            }

            return SHA256.HashData(Encoding.UTF8.GetBytes(_options.ClientSecret));
        }
    }

    public string Protect(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(Key, tag.Length);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);
        return Convert.ToBase64String(result);
    }

    public string Unprotect(string protectedValue)
    {
        var data = Convert.FromBase64String(protectedValue);
        if (data.Length < 28)
        {
            throw new InvalidOperationException("Stored token is not in the expected format.");
        }

        var nonce = data.AsSpan(0, 12);
        var tag = data.AsSpan(12, 16);
        var ciphertext = data.AsSpan(28);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(Key, tag.Length);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
