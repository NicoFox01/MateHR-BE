using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.Entities;
namespace MateHR.Domain.Tenants.Interfaces
{
    public interface ITenantRepository
    {
        Task<Tenant> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Tenant> GetTenantBySlugAsync(string slug, CancellationToken cancellationToken = default);
        Task<List<Tenant>> GetTenantsAsync(TenantStatus? status = null, CancellationToken cancellationToken = default);
        Task<Tenant> CreateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);
        Task<Tenant> UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);
        Task<Tenant> UpdateStatusTenantAsync(Guid tenantId, TenantStatus status, CancellationToken cancellationToken = default);
        Task<Tenant> UpdateRecruitmentModeTenantAsync(Guid tenantId, RecruitmentMode recruitmentMode, CancellationToken cancellationToken = default);
    }
}