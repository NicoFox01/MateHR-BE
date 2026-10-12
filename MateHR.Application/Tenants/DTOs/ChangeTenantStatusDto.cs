using MateHR.Domain.Tenants.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Application.Tenants.DTOs
{
    public class ChangeTenantStatusDto
    {
        public TenantStatus Status { get; set; }
    }
}
