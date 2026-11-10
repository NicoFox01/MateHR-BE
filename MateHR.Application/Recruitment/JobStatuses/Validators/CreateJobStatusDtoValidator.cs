using FluentValidation;
using MateHR.Application.Recruitment.JobStatuses.DTOs;
using MateHR.Domain.Recruitment.Entities;

namespace MateHR.Application.Recruitment.JobStatuses.Validators
{
    public class CreateJobStatusDtoValidator : AbstractValidator<CreateJobStatusDto>
    {
        public CreateJobStatusDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(JobStatus.NameMaxLength);
        }
    }
}
