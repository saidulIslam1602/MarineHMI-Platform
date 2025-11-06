using System.Text.Json;
using System.Text.Json.Serialization;

namespace KChief.Platform.Core.Utilities;

/// <summary>
/// Helper class for JSON operations.
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serializes an object to JSON string.
    /// </summary>
    public static string Serialize<T>(T value, bool indented = false)
    {
        var options = indented ? IndentedOptions : DefaultOptions;
        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Deserializes a JSON string to an object, throwing on error.
    /// </summary>
    public static T DeserializeOrThrow<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON string cannot be null or empty", nameof(json));
        }

        return JsonSerializer.Deserialize<T>(json, DefaultOptions)
            ?? throw new JsonException($"Failed to deserialize JSON to {typeof(T).Name}");
    }

    /// <summary>
    /// Tries to deserialize a JSON string.
    /// </summary>
    public static bool TryDeserialize<T>(string json, out T? result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<T>(json, DefaultOptions);
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clones an object by serializing and deserializing.
    /// </summary>
    public static T? Clone<T>(T value)
    {
        if (value == null)
        {
            return default;
        }

        var json = Serialize(value);
        return Deserialize<T>(json);
    }
}

