using FluentValidation;
using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Domain.Recruitment.Entities;

namespace MateHR.Application.Recruitment.Areas.Validators
{
    public class UpdateAreaDtoValidator : AbstractValidator<UpdateAreaDto>
    {
        public UpdateAreaDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(Area.NameMaxLength);
        }
    }
}
