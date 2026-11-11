using AutoMapper;
using MateHR.Application.Recruitment.JobStatuses.DTOs;
using MateHR.Domain.Common;
using MateHR.Domain.Recruitment.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Application.Recruitment.JobStatuses.Mappings
{
    public class JobStatusMapping : Profile
    {
        public JobStatusMapping()
        {
            CreateMap<JobStatus, JobStatusResponse>();
            CreateMap<PagedResult<JobStatus>, PagedResult<JobStatusResponse>>();
        }
    }
}
