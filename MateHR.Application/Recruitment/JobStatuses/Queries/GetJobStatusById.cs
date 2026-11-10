using AutoMapper;
using MateHR.Application.Recruitment.JobStatuses.DTOs;
using MateHR.Application.Recruitment.JobStatuses.Interfaces;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;


namespace MateHR.Application.Recruitment.JobStatuses.Queries
{
    public class GetJobStatusById : IGetJobStatusById
    {
        private readonly IJobStatusRepository _repository;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;
        public GetJobStatusById(
            IJobStatusRepository repository,
            ITenantContext tenantContext,
            IMapper mapper
            )
        {
            _repository = repository;
            _tenantContext = tenantContext;
            _mapper = mapper;
        }
        public async Task<JobStatusResponse> ExecuteAsync(Guid id, CancellationToken ct = default)
        {
            var tenantId = _tenantContext.CurrentTenantId;

            if (tenantId == null)
            {
                throw new UnauthorizedAccessException("No tienes acceso para acceder a esta información");
            }
            var jobStatus = await _repository.GetByIdAsync(tenantId.Value, id, ct);
            return _mapper.Map<JobStatusResponse>(jobStatus);
        }
    }
}
