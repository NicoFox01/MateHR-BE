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

        public async Task<List<Tenant>> GetTenantsAsync(TenantStatus? status = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Tenants.AsQueryable();
            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<Tenant> CreateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            await _context.Tenants.AddAsync(tenant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return tenant;
        }

        public async Task<Tenant> UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            var existing = await GetTenantByIdAsync(tenant.Id, cancellationToken);

            existing.Update(
                tenant.Name,
                tenant.Slug,
                tenant.CUIT,
                tenant.OwnerEmail,
                tenant.Industry,
                tenant.Address);

            await _context.SaveChangesAsync(cancellationToken);
            return existing;
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
    }
}