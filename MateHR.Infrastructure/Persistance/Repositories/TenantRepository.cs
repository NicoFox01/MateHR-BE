using MateHR.Domain.Common;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MateHR.Infrastructure.Persistance.Repositories
{
    public class TenantRepository : ITenantRepository
    {
        private readonly ApplicationDbContext _context;

        public TenantRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Tenant> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenant is null)
            {
                throw new KeyNotFoundException($"No se encontro el tenant con ID {tenantId}");
            }

            return tenant;
        }

        public async Task<Tenant> GetTenantBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

            if (tenant is null)
            {
                throw new KeyNotFoundException($"No se encontro el tenant con slug {slug}");
            }

            return tenant;
        }

        public async Task<PagedResult<Tenant>> GetTenantsAsync(
            TenantStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Tenants.AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Tenant>(items, pageNumber, pageSize, totalCount);
        }

        public Task<bool> ExistsBySlugAsync(
            string slug,
            Guid? excludeTenantId,
            CancellationToken cancellationToken = default)
        {
            return _context.Tenants
                .AsNoTracking()
                .AnyAsync(
                    t => t.Slug == slug && (excludeTenantId == null || t.Id != excludeTenantId),
                    cancellationToken);
        }

        public async Task<Tenant> CreateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            await _context.Tenants.AddAsync(tenant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return tenant;
        }

        public async Task<Tenant> UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
            return tenant;
        }

        public async Task<Tenant> UpdateStatusTenantAsync(Guid tenantId, TenantStatus status, CancellationToken cancellationToken = default)
        {
            var tenant = await GetTenantByIdAsync(tenantId, cancellationToken);

            tenant.ChangeStatus(status);

            await _context.SaveChangesAsync(cancellationToken);
            return tenant;
        }

        public async Task<Tenant> UpdateRecruitmentModeTenantAsync(Guid tenantId, RecruitmentMode recruitmentMode, CancellationToken cancellationToken = default)
        {
            var tenant = await GetTenantByIdAsync(tenantId, cancellationToken);
            tenant.ChangeRecruitmentMode(recruitmentMode);
            await _context.SaveChangesAsync(cancellationToken);
            return tenant;
        }

        public async Task<Tenant> UpdateSubscriptionTypeTenantAsync(Guid tenantId, SubscriptionType subscriptionType, CancellationToken cancellationToken = default)
        {
            var tenant = await GetTenantByIdAsync(tenantId, cancellationToken);
            tenant.ChangeSubscriptionType(subscriptionType);
            await _context.SaveChangesAsync(cancellationToken);
            return tenant;
        }
    }
}