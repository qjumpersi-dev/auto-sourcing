using System.Security.Cryptography;
using System.Text;

namespace AutoSourcing.Services.Email;

public static class UrlToken
{
    public static string Encode(string url)
    {
        var bytes = Encoding.UTF8.GetBytes(url);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    // Short, stable code for a URL (used in short tracking links like /l/12/7f3a91b2).
    public static string ShortCode(string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(url));

        ulong value = 0;
        for (var i = 0; i < 5; i++)
        {
            value = (value << 8) | hash[i];
        }

        const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
        var builder = new StringBuilder();
        while (value > 0)
        {
            builder.Insert(0, alphabet[(int)(value % 36)]);
            value /= 36;
        }

        return builder.ToString().PadLeft(8, '0');
    }

    public static string? Decode(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            var value = token.Replace('-', '+').Replace('_', '/');
            value = (value.Length % 4) switch
            {
                2 => value + "==",
                3 => value + "=",
                _ => value
            };

            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch
        {
            return null;
        }
    }
}
