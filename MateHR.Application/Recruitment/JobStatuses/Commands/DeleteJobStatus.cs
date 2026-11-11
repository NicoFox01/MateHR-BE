using MateHR.Application.Recruitment.JobStatuses.Interfaces;
using MateHR.Domain.Recruitment.Exceptions;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Recruitment.JobStatuses.Commands
{
    public class DeleteJobStatus : IDeleteJobStatus
    {
        private readonly IJobStatusRepository _repository;
        private readonly ITenantContext _tenantContext;

        public DeleteJobStatus(
            IJobStatusRepository repository,
            ITenantContext tenantContext
            )
        {
            _repository = repository;
            _tenantContext = tenantContext;
        }
        public async Task ExecuteAsync(Guid id, CancellationToken ct = default)
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if(tenantId == null)
            {
                throw new UnauthorizedAccessException("No tienes acceso para acceder a esta información");
            }
            await _repository.GetByIdAsync(tenantId.Value, id, ct);
            var jobStatusInUse = await _repository.IsInUseAsync(tenantId.Value, id, ct);
            if (jobStatusInUse)
            {
                throw new EntityInUseException("JobStatus", id);
            }
            await _repository.DeleteAsync(tenantId.Value, id, ct);

        }
    }
}
