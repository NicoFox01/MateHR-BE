using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Infrastructure.Services
{
    public class TenantContext : ITenantContext
    {
        public Guid? CurrentTenantId { get; private set; }

        public TenantContext()
        {
            CurrentTenantId = null;
        }

        public void SetCurrentTenantId(Guid? tenantId)
        {
            CurrentTenantId = tenantId;
        }
    }
}
