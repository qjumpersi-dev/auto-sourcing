using System.Text;

namespace AutoSourcing.Services.Email;

public static class UrlToken
{
    public static string Encode(string url)
    {
        var bytes = Encoding.UTF8.GetBytes(url);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
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
