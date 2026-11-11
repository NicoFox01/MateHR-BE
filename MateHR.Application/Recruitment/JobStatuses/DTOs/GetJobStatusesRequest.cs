using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Application.Recruitment.JobStatuses.DTOs
{
    public class GetJobStatusesRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
