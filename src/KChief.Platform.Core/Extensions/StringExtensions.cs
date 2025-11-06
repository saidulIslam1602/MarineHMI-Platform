using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace KChief.Platform.Core.Extensions;

/// <summary>
/// Extension methods for string operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if a string is null or empty (including whitespace).
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Checks if a string is not null or empty.
    /// </summary>
    public static bool IsNotNullOrWhiteSpace(this string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Truncates a string to a maximum length.
    /// </summary>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength - suffix.Length) + suffix;
    }

    /// <summary>
    /// Converts a string to title case.
    /// </summary>
    public static string ToTitleCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(value.ToLower());
    }

    /// <summary>
    /// Converts a string to camelCase.
    /// </summary>
    public static string ToCamelCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.Length == 1)
        {
            return value.ToLower();
        }

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    public static string ToPascalCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.Length == 1)
        {
            return value.ToUpper();
        }

        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    /// <summary>
    /// Converts a string to kebab-case.
    /// </summary>
    public static string ToKebabCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return Regex.Replace(value, @"([a-z])([A-Z])", "$1-$2").ToLower();
    }

    /// <summary>
    /// Converts a string to snake_case.
    /// </summary>
    public static string ToSnakeCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return Regex.Replace(value, @"([a-z])([A-Z])", "$1_$2").ToLower();
    }

    /// <summary>
    /// Masks sensitive information in a string (e.g., passwords, tokens).
    /// </summary>
    public static string Mask(this string value, int visibleChars = 4, char maskChar = '*')
    {
        if (string.IsNullOrEmpty(value) || value.Length <= visibleChars)
        {
            return new string(maskChar, value?.Length ?? 0);
        }

        var visible = value.Substring(0, visibleChars);
        var masked = new string(maskChar, value.Length - visibleChars);
        return visible + masked;
    }

    /// <summary>
    /// Removes HTML tags from a string.
    /// </summary>
    public static string StripHtml(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return Regex.Replace(value, "<.*?>", string.Empty);
    }

    /// <summary>
    /// Extracts email addresses from a string.
    /// </summary>
    public static IEnumerable<string> ExtractEmails(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Enumerable.Empty<string>();
        }

        var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
        return Regex.Matches(value, emailPattern, RegexOptions.IgnoreCase)
            .Cast<Match>()
            .Select(m => m.Value)
            .Distinct();
    }

    /// <summary>
    /// Validates if a string matches a pattern.
    /// </summary>
    public static bool MatchesPattern(this string value, string pattern, RegexOptions options = RegexOptions.None)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return Regex.IsMatch(value, pattern, options);
    }

    /// <summary>
    /// Converts a string to a byte array using UTF-8 encoding.
    /// </summary>
    public static byte[] ToByteArray(this string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    /// <summary>
    /// Converts a byte array to a string using UTF-8 encoding.
    /// </summary>
    public static string FromByteArray(this byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Generates a hash code for a string (deterministic).
    /// </summary>
    public static int GetDeterministicHashCode(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        unchecked
        {
            int hash = 17;
            foreach (char c in value)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }

    /// <summary>
    /// Splits a string by a delimiter and trims each part.
    /// </summary>
    public static IEnumerable<string> SplitAndTrim(this string value, char delimiter)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Enumerable.Empty<string>();
        }

        return value.Split(delimiter)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s));
    }

    /// <summary>
    /// Checks if a string contains any of the specified values (case-insensitive).
    /// </summary>
    public static bool ContainsAny(this string value, params string[] values)
    {
        if (string.IsNullOrEmpty(value) || values == null || values.Length == 0)
        {
            return false;
        }

        return values.Any(v => value.Contains(v, StringComparison.OrdinalIgnoreCase));
    }
}

