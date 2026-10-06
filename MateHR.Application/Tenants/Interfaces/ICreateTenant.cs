using MateHR.Application.Tenants.DTOs;

namespace MateHR.Application.Tenants.Interfaces
{
    public interface ICreateTenant
    {
        Task <TenantResponse> ExecuteAsync(CreateTenantDto createTenantDto, CancellationToken cancellationToken = default);
    }
}