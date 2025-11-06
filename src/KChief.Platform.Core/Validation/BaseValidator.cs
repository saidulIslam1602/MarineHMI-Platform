using FluentValidation;

namespace KChief.Platform.Core.Validation;

/// <summary>
/// Base class for validators with common validation rules.
/// </summary>
public abstract class BaseValidator<T> : AbstractValidator<T>
{
    /// <summary>
    /// Validates that a string is not null or empty.
    /// </summary>
    protected IRuleBuilderOptions<T, string> ValidateRequiredString(IRuleBuilder<T, string> ruleBuilder, string fieldName)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage($"{fieldName} is required")
            .WithErrorCode($"{fieldName.ToUpperInvariant()}_REQUIRED");
    }

    /// <summary>
    /// Validates string length.
    /// </summary>
    protected IRuleBuilderOptions<T, string> ValidateStringLength(
        IRuleBuilder<T, string> ruleBuilder,
        string fieldName,
        int minLength,
        int maxLength)
    {
        return ruleBuilder
            .Length(minLength, maxLength)
            .WithMessage($"{fieldName} must be between {minLength} and {maxLength} characters")
            .WithErrorCode($"{fieldName.ToUpperInvariant()}_LENGTH");
    }

    /// <summary>
    /// Validates email format.
    /// </summary>
    protected IRuleBuilderOptions<T, string> ValidateEmail(IRuleBuilder<T, string> ruleBuilder, string fieldName = "Email")
    {
        return ruleBuilder
            .EmailAddress()
            .WithMessage($"{fieldName} must be a valid email address")
            .WithErrorCode($"{fieldName.ToUpperInvariant()}_INVALID_FORMAT");
    }

    /// <summary>
    /// Validates numeric range.
    /// </summary>
    protected IRuleBuilderOptions<T, TProperty> ValidateRange<TProperty>(
        IRuleBuilder<T, TProperty> ruleBuilder,
        string fieldName,
        TProperty min,
        TProperty max)
        where TProperty : IComparable<TProperty>
    {
        return ruleBuilder
            .InclusiveBetween(min, max)
            .WithMessage($"{fieldName} must be between {min} and {max}")
            .WithErrorCode($"{fieldName.ToUpperInvariant()}_RANGE");
    }

    /// <summary>
    /// Validates that a value is greater than a threshold.
    /// </summary>
    protected IRuleBuilderOptions<T, TProperty> ValidateGreaterThan<TProperty>(
        IRuleBuilder<T, TProperty> ruleBuilder,
        string fieldName,
        TProperty threshold)
        where TProperty : IComparable<TProperty>
    {
        return ruleBuilder
            .GreaterThan(threshold)
            .WithMessage($"{fieldName} must be greater than {threshold}")
            .WithErrorCode($"{fieldName.ToUpperInvariant()}_MIN_VALUE");
    }

    /// <summary>
    /// Validates that a value is less than a threshold.
    /// </summary>
    protected IRuleBuilderOptions<T, TProperty> ValidateLessThan<TProperty>(
        IRuleBuilder<T, TProperty> ruleBuilder,
        string fieldName,
        TProperty threshold)
        where TProperty : IComparable<TProperty>
    {
        return ruleBuilder
            .LessThan(threshold)
            .WithMessage($"{fieldName} must be less than {threshold}")
            .WithErrorCode($"{fieldName.ToUpperInvariant()}_MAX_VALUE");
    }

    /// <summary>
    /// Validates date/time is not in the past.
    /// </summary>
    protected IRuleBuilderOptions<T, DateTime> ValidateNotInPast(IRuleBuilder<T, DateTime> ruleBuilder, string fieldName = "Date")
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(DateTime.UtcNow)
            .WithMessage($"{fieldName} cannot be in the past")
            .WithErrorCode($"{fieldName.ToUpperInvariant()}_IN_PAST");
    }

    /// <summary>
    /// Validates date/time is not in the future.
    /// </summary>
    protected IRuleBuilderOptions<T, DateTime> ValidateNotInFuture(IRuleBuilder<T, DateTime> ruleBuilder, string fieldName = "Date")
    {
        return ruleBuilder
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage($"{fieldName} cannot be in the future")
            .WithErrorCode($"{fieldName.ToUpperInvariant()}_IN_FUTURE");
    }

    /// <summary>
    /// Validates GUID format.
    /// </summary>
    protected IRuleBuilderOptions<T, string> ValidateGuid(IRuleBuilder<T, string> ruleBuilder, string fieldName = "Id")
    {
        return ruleBuilder
            .Must(guid => Guid.TryParse(guid, out _))
            .WithMessage($"{fieldName} must be a valid GUID")
            .WithErrorCode($"{fieldName.ToUpperInvariant()}_INVALID_FORMAT");
    }

    /// <summary>
    /// Validates URL format.
    /// </summary>
    protected IRuleBuilderOptions<T, string> ValidateUrl(IRuleBuilder<T, string> ruleBuilder, string fieldName = "Url")
    {
        return ruleBuilder
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage($"{fieldName} must be a valid URL")
            .WithErrorCode($"{fieldName.ToUpperInvariant()}_INVALID_FORMAT");
    }
}

