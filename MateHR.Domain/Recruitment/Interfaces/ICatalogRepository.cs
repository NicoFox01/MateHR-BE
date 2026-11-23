using MateHR.Domain.Common;
using MateHR.Domain.Recruitment.Common;

namespace MateHR.Domain.Recruitment.Interfaces
{
    public interface ICatalogRepository<TEntity> where TEntity : class, ICatalogEntity
    {
        Task<TEntity> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<PagedResult<TEntity>> GetAllAsync(Guid tenantId, int pageNumber, int pageSize, bool includeInactive = false, CancellationToken ct = default);
        Task<bool> NameExistsAsync(Guid tenantId, string name, Guid? excludeId, CancellationToken ct = default);
        Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default);
        Task<TEntity> RenameAsync(Guid tenantId, Guid id, string name, CancellationToken ct = default);
        Task<TEntity> SetActiveAsync(Guid tenantId, Guid id, bool isActive, CancellationToken ct = default);
        Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<bool> IsInUseAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    }
}