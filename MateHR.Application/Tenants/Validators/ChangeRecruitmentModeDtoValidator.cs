using FluentValidation;
using MateHR.Application.Tenants.DTOs;

namespace MateHR.Application.Tenants.Validators
{
    public class ChangeRecruitmentModeDtoValidator:AbstractValidator<ChangeRecruitmentModeDto>
    {
        public ChangeRecruitmentModeDtoValidator()
        {
            RuleFor(t => t.RecruitmentMode).IsInEnum();
        }
    }
}
