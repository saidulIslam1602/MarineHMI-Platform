using FluentValidation;
using KChief.Platform.API.Controllers;
using KChief.Platform.Core.Validation;

namespace KChief.Platform.API.Validators;

/// <summary>
/// Validator for LoginRequest using shared validation framework.
/// </summary>
public class LoginRequestValidator : BaseValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .ValidateRequiredString("Username")
            .ValidateStringLength("Username", 3, 255)
            .Matches(@"^[a-zA-Z0-9_\-\.]+$")
            .WithMessage("Username can only contain letters, numbers, underscores, hyphens, and dots")
            .WithErrorCode("LOGIN_USERNAME_INVALID_FORMAT")
            .When(x => !string.IsNullOrEmpty(x.Username));

        RuleFor(x => x.Password)
            .ValidateRequiredString("Password")
            .ValidateStringLength("Password", 6, 255);
    }
}

