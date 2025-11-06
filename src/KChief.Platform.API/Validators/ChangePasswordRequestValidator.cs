using FluentValidation;
using KChief.Platform.API.Controllers;

namespace KChief.Platform.API.Validators;

/// <summary>
/// Validator for ChangePasswordRequest.
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required")
            .WithErrorCode("CHANGE_PASSWORD_CURRENT_REQUIRED")
            .MinimumLength(6)
            .WithMessage("Current password must be at least 6 characters long")
            .WithErrorCode("CHANGE_PASSWORD_CURRENT_MIN_LENGTH");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .WithErrorCode("CHANGE_PASSWORD_NEW_REQUIRED")
            .MinimumLength(8)
            .WithMessage("New password must be at least 8 characters long")
            .WithErrorCode("CHANGE_PASSWORD_NEW_MIN_LENGTH")
            .MaximumLength(255)
            .WithMessage("New password must not exceed 255 characters")
            .WithErrorCode("CHANGE_PASSWORD_NEW_MAX_LENGTH")
            .Matches(@"[A-Z]")
            .WithMessage("New password must contain at least one uppercase letter")
            .WithErrorCode("CHANGE_PASSWORD_NEW_UPPERCASE")
            .Matches(@"[a-z]")
            .WithMessage("New password must contain at least one lowercase letter")
            .WithErrorCode("CHANGE_PASSWORD_NEW_LOWERCASE")
            .Matches(@"[0-9]")
            .WithMessage("New password must contain at least one digit")
            .WithErrorCode("CHANGE_PASSWORD_NEW_DIGIT")
            .Matches(@"[^a-zA-Z0-9]")
            .WithMessage("New password must contain at least one special character")
            .WithErrorCode("CHANGE_PASSWORD_NEW_SPECIAL")
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must be different from current password")
            .WithErrorCode("CHANGE_PASSWORD_NEW_DIFFERENT");
    }
}

