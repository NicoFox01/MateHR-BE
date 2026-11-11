using FluentValidation;
using MateHR.Application.Recruitment.JobStatuses.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Application.Recruitment.JobStatuses.Validators
{
    public class GetJobStatusesRequestValidator : AbstractValidator<GetJobStatusesRequest>
    {
        public GetJobStatusesRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(100);
        }
    }
}
