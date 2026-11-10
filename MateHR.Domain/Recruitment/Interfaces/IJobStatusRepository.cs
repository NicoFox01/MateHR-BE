using MateHR.Domain.Common;
using MateHR.Domain.Recruitment.Entities;


namespace MateHR.Domain.Recruitment.Interfaces
{
    public interface IJobStatusRepository
    {
        Task<JobStatus> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<PagedResult<JobStatus>> GetAllAsync(Guid tenantId, int pageNumber, int pageSize, CancellationToken ct = default);
        Task<bool> NameExistsAsync(Guid tenantId, string name, Guid? excludeId, CancellationToken ct = default);
        Task<JobStatus> CreateAsync(JobStatus entity, CancellationToken ct = default);
        Task<JobStatus> RenameAsync(Guid tenantId, Guid id, string name, CancellationToken ct = default);
        Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default);
        Task<bool> IsInUseAsync(Guid tenantId, Guid id, CancellationToken ct = default);

    }
}
