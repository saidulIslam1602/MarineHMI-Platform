using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace KChief.Platform.Core.Utilities;

/// <summary>
/// Helper class for validation operations.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates an email address.
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a URL.
    /// </summary>
    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validates a GUID.
    /// </summary>
    public static bool IsValidGuid(string? guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
        {
            return false;
        }

        return Guid.TryParse(guid, out _);
    }

    /// <summary>
    /// Validates a phone number (basic validation).
    /// </summary>
    public static bool IsValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        // Basic phone number pattern (can be customized)
        var pattern = @"^\+?[1-9]\d{1,14}$";
        return Regex.IsMatch(phoneNumber, pattern);
    }

    /// <summary>
    /// Validates a maritime vessel ID format.
    /// </summary>
    public static bool IsValidVesselId(string? vesselId)
    {
        if (string.IsNullOrWhiteSpace(vesselId))
        {
            return false;
        }

        var pattern = @"^vessel-\d{3}$";
        return Regex.IsMatch(vesselId, pattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Validates an engine ID format.
    /// </summary>
    public static bool IsValidEngineId(string? engineId)
    {
        if (string.IsNullOrWhiteSpace(engineId))
        {
            return false;
        }

        var pattern = @"^engine-\d{3}$";
        return Regex.IsMatch(engineId, pattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Validates an object using DataAnnotations.
    /// </summary>
    public static ValidationResult ValidateObject(object obj)
    {
        var context = new ValidationContext(obj);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(obj, context, results, true);

        return new ValidationResult
        {
            IsValid = isValid,
            Errors = results.Select(r => r.ErrorMessage ?? string.Empty).ToList()
        };
    }

    /// <summary>
    /// Validates a property value.
    /// </summary>
    public static ValidationResult ValidateProperty(object obj, string propertyName)
    {
        var context = new ValidationContext(obj) { MemberName = propertyName };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var property = obj.GetType().GetProperty(propertyName);
        
        if (property == null)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { $"Property '{propertyName}' not found" }
            };
        }

        var value = property.GetValue(obj);
        var isValid = Validator.TryValidateProperty(value, context, results);

        return new ValidationResult
        {
            IsValid = isValid,
            Errors = results.Select(r => r.ErrorMessage ?? string.Empty).ToList()
        };
    }
}

/// <summary>
/// Validation result.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

