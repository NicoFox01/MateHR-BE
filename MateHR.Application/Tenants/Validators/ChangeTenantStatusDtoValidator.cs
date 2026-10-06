using FluentValidation;
using MateHR.Application.Tenants.DTOs;


namespace MateHR.Application.Tenants.Validators
{
    public class ChangeTenantStatusDtoValidator: AbstractValidator<ChangeTenantStatusDto>
    {
        public ChangeTenantStatusDtoValidator()
        {
            RuleFor(t => t.Status).IsInEnum();
        }
    }
}
