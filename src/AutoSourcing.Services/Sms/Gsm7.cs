using System.Globalization;
using System.Text;

namespace AutoSourcing.Services.Sms;

// Keeps SMS bodies to plain ASCII so Twilio always sends GSM-7 (160-character) segments instead
// of 70-character UCS-2 segments. Smart punctuation and accented letters are converted to their
// ASCII equivalents; anything that cannot be represented is removed.
public static class Gsm7
{
    public static string ToAscii(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var rune in normalized.EnumerateRunes())
        {
            var ch = rune.Value;

            if (IsAllowed(ch))
            {
                sb.Append((char)ch);
                continue;
            }

            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var mapped = Map(ch);
            if (mapped is not null)
            {
                sb.Append(mapped);
            }
        }

        return sb.ToString();
    }

    private static bool IsAllowed(int ch) =>
        ch == '\n' || ch == '\r' || ch == '\t' || (ch >= 0x20 && ch <= 0x7E);

    // Characters that would otherwise force a 70-character UCS-2 segment.
    private static string? Map(int ch) => ch switch
    {
        0x2018 or 0x2019 or 0x201A or 0x201B => "'",
        0x201C or 0x201D or 0x201E or 0x201F => "\"",
        0x2010 or 0x2011 or 0x2012 or 0x2013 or 0x2014 or 0x2015 => "-",
        0x2026 or 0x2025 or 0x22EF => "...",
        0x2022 or 0x00B7 or 0x2023 or 0x2043 or 0x25AA => "-",
        0x2190 => "<-",
        0x2192 => "->",
        0x2191 or 0x2193 => "|",
        0x00A9 => "(c)",
        0x00AE => "(r)",
        0x2122 => "(tm)",
        0x2020 or 0x2021 => "*",
        0x2032 => "'",
        0x2033 => "\"",
        _ => null
    };
}