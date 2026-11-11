using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Application.Recruitment.JobStatuses.DTOs
{
    public class JobStatusResponse
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
