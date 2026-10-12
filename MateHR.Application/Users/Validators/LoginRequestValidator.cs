using FluentValidation;
using MateHR.Application.Users.DTOs;
using MateHR.Domain.Users.Entities;

namespace MateHR.Application.Users.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .MaximumLength(User.EmailMaxLength)
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128);
        }
    }
}