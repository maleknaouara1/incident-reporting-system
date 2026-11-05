using FluentValidation;
using IncidentReporting.Application.Dtos;

namespace IncidentReporting.Application.Incidents.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}
