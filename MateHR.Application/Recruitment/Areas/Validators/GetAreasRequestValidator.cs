using FluentValidation;
using MateHR.Application.Recruitment.Areas.DTOs;

namespace MateHR.Application.Recruitment.Areas.Validators
{
    public class GetAreasRequestValidator : AbstractValidator<GetAreasRequest>
    {
        public GetAreasRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(100);
        }
    }
}
