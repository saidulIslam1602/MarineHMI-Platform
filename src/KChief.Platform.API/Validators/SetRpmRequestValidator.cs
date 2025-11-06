using FluentValidation;
using KChief.Platform.Core.Interfaces;
using KChief.Platform.Core.Models;
using KChief.Platform.API.Controllers;

namespace KChief.Platform.API.Validators;

/// <summary>
/// Validator for SetRpmRequest.
/// </summary>
public class SetRpmRequestValidator : AbstractValidator<SetRpmRequest>
{
    public SetRpmRequestValidator()
    {
        RuleFor(x => x.Rpm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("RPM must be greater than or equal to 0")
            .WithErrorCode("SET_RPM_MIN_VALUE")
            .LessThanOrEqualTo(10000)
            .WithMessage("RPM must be less than or equal to 10000")
            .WithErrorCode("SET_RPM_MAX_VALUE");
    }
}

/// <summary>
/// Extended validator for SetRpmRequest that validates against engine constraints.
/// </summary>
public class SetRpmRequestWithEngineValidator : AbstractValidator<(SetRpmRequest Request, Engine Engine)>
{
    public SetRpmRequestWithEngineValidator()
    {
        RuleFor(x => x.Request.Rpm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("RPM must be greater than or equal to 0")
            .WithErrorCode("SET_RPM_MIN_VALUE")
            .LessThanOrEqualTo(x => x.Engine.MaxRPM)
            .WithMessage(x => $"RPM must be less than or equal to the engine's maximum RPM ({x.Engine.MaxRPM})")
            .WithErrorCode("SET_RPM_EXCEEDS_MAX")
            .When(x => x.Engine != null);

        RuleFor(x => x.Engine)
            .NotNull()
            .WithMessage("Engine must exist")
            .WithErrorCode("SET_RPM_ENGINE_NOT_FOUND")
            .Must(engine => engine.Status == EngineStatus.Running)
            .WithMessage("Engine must be running to set RPM")
            .WithErrorCode("SET_RPM_ENGINE_NOT_RUNNING")
            .When(x => x.Engine != null);
    }
}

