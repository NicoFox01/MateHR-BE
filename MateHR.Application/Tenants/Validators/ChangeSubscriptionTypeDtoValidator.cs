using FluentValidation;
using MateHR.Application.Tenants.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Application.Tenants.Validators
{
    public class ChangeSubscriptionTypeDtoValidator:AbstractValidator<ChangeSubscriptionTypeDto>
    {
        public ChangeSubscriptionTypeDtoValidator()
        {
            RuleFor(t => t.SubscriptionType).IsInEnum();
        }
    }
}
