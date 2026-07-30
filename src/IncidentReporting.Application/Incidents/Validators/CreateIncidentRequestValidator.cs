using FluentValidation;
using IncidentReporting.Application.Incidents.Dtos;

namespace IncidentReporting.Application.Incidents.Validators;

public class CreateIncidentRequestValidator : AbstractValidator<CreateIncidentRequest>
{
    public CreateIncidentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(4000);

       
        RuleFor(x => x.Priority)
            .IsInEnum();

        RuleFor(x => x.Category)
            .IsInEnum();
    }
}