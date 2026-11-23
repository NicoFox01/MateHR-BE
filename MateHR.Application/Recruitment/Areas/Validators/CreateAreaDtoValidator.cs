using FluentValidation;
using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Domain.Recruitment.Entities;

namespace MateHR.Application.Recruitment.Areas.Validators
{
    public class CreateAreaDtoValidator : AbstractValidator<CreateAreaDto>
    {
        public CreateAreaDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(Area.NameMaxLength);
        }
    }
}
