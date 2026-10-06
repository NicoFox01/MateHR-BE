using MateHR.Application.Tenants.DTOs;

namespace MateHR.Application.Tenants.Interfaces
{
    public interface IChangeStatusTenant
    {
        Task <TenantResponse> ExecuteAsync(Guid tenantId, ChangeTenantStatusDto changeTenantStatusDto, CancellationToken cancellationToken = default);
    }
}