using FluentValidation;
using MateHR.Application.Tenants.DTOs;

namespace MateHR.Application.Tenants.Validators
{
    public class ChangeSubscriptionTypeDtoValidator : AbstractValidator<ChangeSubscriptionTypeDto>
    {
        public ChangeSubscriptionTypeDtoValidator()
        {
            RuleFor(t => t.SubscriptionType).IsInEnum();
        }
    }
}
