using MateHR.Application.Tenants.DTOs;
using MateHR.Domain.Common;

namespace MateHR.Application.Tenants.Interfaces
{
    public interface IGetTenants
    {
        Task<PagedResult<TenantResponse>> ExecuteAsync(GetTenantsRequest request, CancellationToken cancellationToken = default);
    }
}