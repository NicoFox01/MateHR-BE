using MateHR.Domain.Common;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.Entities;
namespace MateHR.Domain.Tenants.Interfaces
{
    public interface ITenantRepository
    {
        Task<Tenant> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Tenant> GetTenantBySlugAsync(string slug, CancellationToken cancellationToken = default);
        Task<PagedResult<Tenant>> GetTenantsAsync(TenantStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<bool> ExistsBySlugAsync(string slug, Guid? excludeTenantId, CancellationToken cancellationToken = default);
        Task<Tenant> CreateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);
        Task<Tenant> UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);
        Task<Tenant> UpdateStatusTenantAsync(Guid tenantId, TenantStatus status, CancellationToken cancellationToken = default);
        Task<Tenant> UpdateRecruitmentModeTenantAsync(Guid tenantId, RecruitmentMode recruitmentMode, CancellationToken cancellationToken = default);
        Task<Tenant> UpdateSubscriptionTypeTenantAsync(Guid tenantId, SubscriptionType subscriptionType, CancellationToken cancellationToken = default);
    }
}