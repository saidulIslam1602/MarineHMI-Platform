using FluentValidation;
using Microsoft.Extensions.Localization;

namespace KChief.Platform.API.Validators;

/// <summary>
/// Extension methods for localized validation messages.
/// </summary>
public static class LocalizedValidatorExtensions
{
    /// <summary>
    /// Gets a localized error message for a validation error code.
    /// </summary>
    public static string GetLocalizedMessage(this IStringLocalizer localizer, string errorCode, string defaultMessage)
    {
        var localizedKey = $"Validation.{errorCode}";
        var localizedMessage = localizer[localizedKey];
        
        // If no localization found, return default message
        if (localizedMessage.ResourceNotFound)
        {
            return defaultMessage;
        }
        
        return localizedMessage.Value;
    }
}

/// <summary>
/// Base class for validators with localization support.
/// </summary>
public abstract class LocalizedValidatorBase<T> : AbstractValidator<T>
{
    protected readonly IStringLocalizer? Localizer;

    protected LocalizedValidatorBase(IStringLocalizer? localizer = null)
    {
        Localizer = localizer;
    }

    /// <summary>
    /// Gets a localized error message.
    /// </summary>
    protected string GetLocalizedMessage(string errorCode, string defaultMessage)
    {
        if (Localizer == null)
        {
            return defaultMessage;
        }

        return Localizer.GetLocalizedMessage(errorCode, defaultMessage);
    }
}

