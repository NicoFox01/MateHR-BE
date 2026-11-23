using MateHR.Domain.Common;
using MateHR.Domain.Recruitment.Common;
using MateHR.Domain.Recruitment.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MateHR.Infrastructure.Persistance.Repositories.Recruitment
{
    public abstract class CatalogRepository<TEntity> : ICatalogRepository<TEntity>
        where TEntity : class, ICatalogEntity
    {
        protected readonly ApplicationDbContext _context;
        protected CatalogRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        protected DbSet<TEntity> Set => _context.Set<TEntity>();

        public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default)
        {
            await Set.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var entity = await Set.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);
            if (entity is null)
            {
                throw new KeyNotFoundException($"No se encontro la entidad con ID: {id}");
            }
            Set.Remove(entity);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<PagedResult<TEntity>> GetAllAsync(Guid tenantId, int pageNumber, int pageSize, bool includeInactive = false, CancellationToken ct = default)
        {
            var query = Set.AsNoTracking().Where(e => e.TenantId == tenantId);
            if (!includeInactive)
            {
                query = query.Where(e => e.IsActive);
            }
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderBy(e => e.Name)
                .ThenBy(e => e.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
            return new PagedResult<TEntity>(items, pageNumber, pageSize, totalCount);
        }

        public async Task<TEntity> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var entity = await Set.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);
            if (entity is null)
            {
                throw new KeyNotFoundException($"No se encontro la entidad con ID: {id}");
            }
            return entity;
        }

        public virtual Task<bool> IsInUseAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        {
            return Task.FromResult(false);
        }

        public async Task<bool> NameExistsAsync(Guid tenantId, string name, Guid? excludeId, CancellationToken ct = default)
        {
            var normalized = name.Trim().ToLowerInvariant();
            var query = Set.AsNoTracking().Where(e => e.TenantId == tenantId && e.Name.ToLower() == normalized);
            if (excludeId.HasValue)
            {
                query = query.Where(e => e.Id != excludeId.Value);
            }
            return await query.AnyAsync(ct);
        }

        public async Task<TEntity> RenameAsync(Guid tenantId, Guid id, string name, CancellationToken ct = default)
        {
            var entity = await Set.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);
            if (entity is null)
            {
                throw new KeyNotFoundException($"No se encontro la entidad con ID: {id}");
            }
            entity.Rename(name);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<TEntity> SetActiveAsync(Guid tenantId, Guid id, bool isActive, CancellationToken ct = default)
        {
            var entity = await Set.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct);
            if (entity is null)
            {
                throw new KeyNotFoundException($"No se encontro la entidad con ID: {id}");
            }
            if (isActive)
            {
                entity.Activate();
            }
            else
            {
                entity.Deactivate();
            }
            await _context.SaveChangesAsync(ct);
            return entity;
        }
    }
}
