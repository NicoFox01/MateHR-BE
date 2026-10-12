using FluentValidation;
using MateHR.Application.Users.DTOs;
using MateHR.Domain.Users.Enums;

namespace MateHR.Application.Users.Validators
{
    public class GetUsersRequestValidator : AbstractValidator<GetUsersRequest>
    {
        public GetUsersRequestValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEqual(Guid.Empty)
                .When(x => x.TenantId.HasValue)
                .WithMessage("El tenant no puede ser un Guid vacio.");

            RuleFor(x => x.Role)
                .Must(role => !role.HasValue || Enum.IsDefined(typeof(UserRole), role.Value))
                .WithMessage("El rol no es valido.");
        }
    }
}
