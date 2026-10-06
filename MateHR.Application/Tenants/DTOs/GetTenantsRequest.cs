using MateHR.Domain.Tenants.Enums;

namespace MateHR.Application.Tenants.DTOs
{
    public class GetTenantsRequest
    {
        public TenantStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}