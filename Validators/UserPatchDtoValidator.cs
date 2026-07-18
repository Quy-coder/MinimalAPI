using FluentValidation;
using MinimalAPIs.Models;

namespace MinimalAPIs.Validators;

// UserCreateDto validate bằng DataAnnotations + AddValidation() (xem Program.cs, Models/UserDtos.cs).
// UserPatchDto validate bằng FluentValidation để so sánh 2 cách "gắn" validator vào request pipeline:
// - Minimal API: bọc validator trong IEndpointFilter (xem Filters/ValidationEndpointFilter.cs).
// - Controller: inject IValidator<T> qua DI, gọi thủ công trong action (xem UsersController.Patch).
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
