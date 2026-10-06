using MateHR.Application.Tenants.DTOs;

namespace MateHR.Application.Tenants.Interfaces
{
    public interface IGetTenantById
    {
        Task <TenantResponse> ExecuteAsync (Guid tenantId, CancellationToken cancellationToken = default);
    }
}