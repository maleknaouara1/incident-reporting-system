using FluentValidation;
using IncidentReporting.Application.Incidents.Dtos;

namespace IncidentReporting.Application.Incidents.Validators;

public class EmailIncidentRequestValidator : AbstractValidator<EmailIncidentRequest>
{
    public EmailIncidentRequestValidator()
    {
        RuleFor(x => x.From)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(20000);

        RuleFor(x => x.MessageId)
            .NotEmpty()
            .MaximumLength(450);

        RuleFor(x => x.ReceivedAtUtc)
            .NotEqual(default(DateTime))
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddMinutes(5))
            .WithMessage("ReceivedAtUtc cannot be in the future.");

        RuleFor(x => x.AiAnalysis)
            .NotNull();

        RuleFor(x => x.AiAnalysis.Summary)
            .NotEmpty()
            .MaximumLength(2000)
            .When(x => x.AiAnalysis is not null);

        RuleFor(x => x.AiAnalysis.SuggestedResolution)
            .MaximumLength(4000)
            .When(x => x.AiAnalysis is not null);

        RuleFor(x => x.AiAnalysis.SuggestedCategory)
            .IsInEnum()
            .When(x => x.AiAnalysis is not null);

        RuleFor(x => x.AiAnalysis.SuggestedPriority)
            .IsInEnum()
            .When(x => x.AiAnalysis is not null);

        RuleFor(x => x.AiAnalysis.ConfidenceScore)
            .InclusiveBetween(0.0, 1.0)
            .When(x => x.AiAnalysis is not null);
    }
}
