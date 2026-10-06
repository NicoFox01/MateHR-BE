using MateHR.Application.Tenants.DTOs;

namespace MateHR.Application.Tenants.Interfaces
{
    public interface IUpdateTenant
    {
        Task <TenantResponse> ExecuteAsync(Guid tenantId, UpdateTenantDto updateTenantDto, CancellationToken cancellationToken = default);
    }
}