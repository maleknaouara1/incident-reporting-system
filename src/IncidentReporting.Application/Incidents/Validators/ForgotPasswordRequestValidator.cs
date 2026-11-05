using FluentValidation;
using IncidentReporting.Application.Dtos;

namespace IncidentReporting.Application.Incidents.Validators;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
