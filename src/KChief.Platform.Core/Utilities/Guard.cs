namespace KChief.Platform.Core.Utilities;

/// <summary>
/// Guard clauses for parameter validation.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws ArgumentNullException if value is null.
    /// </summary>
    public static void AgainstNull<T>(T? value, string parameterName) where T : class
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    /// <summary>
    /// Throws ArgumentException if string is null or empty.
    /// </summary>
    public static void AgainstNullOrEmpty(string? value, string parameterName)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException($"Parameter '{parameterName}' cannot be null or empty", parameterName);
        }
    }

    /// <summary>
    /// Throws ArgumentException if string is null or whitespace.
    /// </summary>
    public static void AgainstNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Parameter '{parameterName}' cannot be null or whitespace", parameterName);
        }
    }

    /// <summary>
    /// Throws ArgumentOutOfRangeException if value is out of range.
    /// </summary>
    public static void AgainstOutOfRange<T>(T value, T min, T max, string parameterName) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"Value must be between {min} and {max}");
        }
    }

    /// <summary>
    /// Throws ArgumentException if value is less than minimum.
    /// </summary>
    public static void AgainstLessThan<T>(T value, T minimum, string parameterName) where T : IComparable<T>
    {
        if (value.CompareTo(minimum) < 0)
        {
            throw new ArgumentException($"Value must be greater than or equal to {minimum}", parameterName);
        }
    }

    /// <summary>
    /// Throws ArgumentException if value is greater than maximum.
    /// </summary>
    public static void AgainstGreaterThan<T>(T value, T maximum, string parameterName) where T : IComparable<T>
    {
        if (value.CompareTo(maximum) > 0)
        {
            throw new ArgumentException($"Value must be less than or equal to {maximum}", parameterName);
        }
    }

    /// <summary>
    /// Throws ArgumentException if collection is null or empty.
    /// </summary>
    public static void AgainstNullOrEmpty<T>(IEnumerable<T>? collection, string parameterName)
    {
        if (collection == null || !collection.Any())
        {
            throw new ArgumentException($"Collection '{parameterName}' cannot be null or empty", parameterName);
        }
    }

    /// <summary>
    /// Throws ArgumentException if condition is false.
    /// </summary>
    public static void Against(bool condition, string message, string parameterName)
    {
        if (condition)
        {
            throw new ArgumentException(message, parameterName);
        }
    }

    /// <summary>
    /// Throws InvalidOperationException if condition is false.
    /// </summary>
    public static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

