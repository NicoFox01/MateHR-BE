using MateHR.Application.Tenants.DTOs;

namespace MateHR.Application.Tenants.Interfaces
{
    public interface IGetTenantBySlug
    {
        Task <TenantResponse> ExecuteAsync(string slug, CancellationToken cancellationToken = default);
    }
}