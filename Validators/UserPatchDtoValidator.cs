using FluentValidation;
using MinimalAPIs.Models;

namespace MinimalAPIs.Validators;

// UserCreateDto is validated with DataAnnotations + AddValidation() (see Program.cs, Models/UserDtos.cs).
// UserPatchDto is validated with FluentValidation to compare 2 ways of "attaching" a validator to the request pipeline:
// - Minimal API: wrap the validator in an IEndpointFilter (see Filters/ValidationEndpointFilter.cs).
// - Controller: inject IValidator<T> via DI, call it manually in the action (see UsersController.Patch).
public class UserPatchDtoValidator : AbstractValidator<UserPatchDto>
{
    public UserPatchDtoValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2)
            .When(x => x.Name is not null);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => x.Email is not null);

        RuleFor(x => x.Age)
            .InclusiveBetween(0, 150)
            .When(x => x.Age.HasValue);

        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Email is not null || x.Age.HasValue)
            .WithMessage("At least one of Name, Email or Age must be provided.")
            .WithName("Patch");
    }
}
