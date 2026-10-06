using FluentValidation;
using MateHR.Application.Users.DTOs;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;

namespace MateHR.Application.Users.Validators
{
    public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(User.FirstNameMaxLength);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(User.LastNameMaxLength);

            RuleFor(x => x.Email)
                .NotEmpty()
                .MaximumLength(User.EmailMaxLength)
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128);

            RuleFor(x => x.Role)
                .IsInEnum()
                .WithMessage("El rol no es valido.");

            // Misma invariante que impone la entidad User, pero con mensaje util
            // en el borde de la API en vez de una excepcion de dominio.
            When(x => x.Role == UserRole.SuperAdmin, () =>
            {
                RuleFor(x => x.TenantId)
                    .Must(tenantId => !tenantId.HasValue)
                    .WithMessage("Un SuperAdmin no pertenece a un tenant.");
            });

            When(x => x.Role != UserRole.SuperAdmin, () =>
            {
                RuleFor(x => x.TenantId)
                    .NotEmpty()
                    .WithMessage("Todo usuario con rol distinto de SuperAdmin debe pertenecer a un tenant.")
                    .NotEqual(Guid.Empty)
                    .WithMessage("El tenant no puede ser un Guid vacio.");
            });
        }
    }
}