using MateHR.Domain.Common;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace MateHR.Infrastructure.Persistance.Repositories
{
    public class JobStatusRepository : IJobStatusRepository
    {
        private readonly ApplicationDbContext _context;
        public JobStatusRepository (ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<JobStatus> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var entity = await _context.JobStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
            if (entity is null)
            {
                throw new KeyNotFoundException($"No se encontro el estado con ID {id}");
            }
            return entity;
        }
        public async Task<PagedResult<JobStatus>> GetAllAsync(Guid tenantId, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = _context.JobStatuses.AsNoTracking()
                .Where(js => js.TenantId == tenantId);
            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderBy(js => js.Name)
                .ThenBy(js => js.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
            return new PagedResult<JobStatus>(items, pageNumber, pageSize, totalCount);
        }
        public async Task<bool> NameExistsAsync(Guid tenantId, string name, Guid? excludeId, CancellationToken ct = default)
        {
            var normalized = name.Trim().ToLowerInvariant();
            var query = _context.JobStatuses.AsNoTracking()
                .Where(js => js.TenantId == tenantId && js.Name.ToLower() == normalized);
            if (excludeId.HasValue)
            {
                query = query.Where(js => js.Id != excludeId.Value);
            }
            return await query.AnyAsync(ct);
        }
        public async Task<JobStatus> CreateAsync(JobStatus entity, CancellationToken ct = default)
        {
            await _context.JobStatuses.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);
            return entity;
        }
        public async Task<JobStatus> RenameAsync(Guid tenantId, Guid id, string name, CancellationToken ct = default)
        {
            var entity = await _context.JobStatuses
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);

            if (entity == null)
            {
                throw new KeyNotFoundException($"No se encontro el estado con ID {id}");
            }
            entity.Rename(name);
            await _context.SaveChangesAsync(ct);
            return entity;

        }
        public async Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var entity = await _context.JobStatuses
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);

            if (entity is null)
            {
                throw new KeyNotFoundException($"No se encontro el estado con ID {id}");
            }

            _context.JobStatuses.Remove(entity);
            await _context.SaveChangesAsync(ct);
        }
        // La entidad Job no existe todavia (REC-08); cuando exista se consulta si algun job del tenant referencia este estado.
        public Task<bool> IsInUseAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        {
            return Task.FromResult(false);
        }
    }
}
