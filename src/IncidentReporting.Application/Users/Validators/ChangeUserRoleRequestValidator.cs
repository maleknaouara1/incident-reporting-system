using FluentValidation;
using IncidentReporting.Application.Users.Dtos;

namespace IncidentReporting.Application.Users.Validators;

public class ChangeUserRoleRequestValidator : AbstractValidator<ChangeUserRoleRequest>
{
    public ChangeUserRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum();
    }
}
