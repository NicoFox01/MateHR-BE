using MateHR.Domain.Tenants.Enums;

namespace MateHR.Application.Tenants.DTOs
{
    public class ChangeTenantStatusDto
    {
        public TenantStatus Status { get; set; }
    }
}
