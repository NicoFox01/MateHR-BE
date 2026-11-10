using FluentValidation;
using MateHR.Application.Recruitment.JobStatuses.DTOs;
using MateHR.Domain.Recruitment.Entities;

namespace MateHR.Application.Recruitment.JobStatuses.Validators
{
    public class UpdateJobStatusDtoValidator : AbstractValidator<UpdateJobStatusDto>
    {
        public UpdateJobStatusDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(JobStatus.NameMaxLength);
        }
    }
}
