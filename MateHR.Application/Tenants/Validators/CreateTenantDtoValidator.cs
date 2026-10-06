using FluentValidation;
using MateHR.Application.Tenants.DTOs;
using MateHR.Domain.Tenants.Entities;


namespace MateHR.Application.Tenants.Validators
{
    public class CreateTenantDtoValidator : AbstractValidator<CreateTenantDto>
    {
        public CreateTenantDtoValidator()
        {
            RuleFor(t => t.Name).NotEmpty().MaximumLength(Tenant.NameMaxLength);
            RuleFor(t => t.CUIT)
                .NotEmpty()
                .Length(Tenant.CuitMaxLength)
                .Matches("^[0-9]+$");
            RuleFor(t =>t.OwnerEmail).NotEmpty().MaximumLength(Tenant.OwnerEmailMaxLength).EmailAddress();
            RuleFor(t => t.Industry).NotNull().IsInEnum();
            RuleFor(t => t.Address!)
                .SetValidator(new AddressDtoValidator())
                .When(t => t.Address is not null);
        }
    }
}
