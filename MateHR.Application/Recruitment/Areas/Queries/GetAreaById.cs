using AutoMapper;
using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Application.Recruitment.Areas.Interfaces;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Recruitment.Areas.Queries
{
    public class GetAreaById : IGetAreaById
    {
        private readonly IAreaRepository _repository;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;

        public GetAreaById(
            IAreaRepository repository,
            ITenantContext tenantContext,
            IMapper mapper)
        {
            _repository = repository;
            _tenantContext = tenantContext;
            _mapper = mapper;
        }

        public async Task<AreaResponse> ExecuteAsync(Guid id, CancellationToken ct = default)
        {
            var tenant = _tenantContext.CurrentTenantId;
            if (tenant == null)
            {
                throw new UnauthorizedAccessException("No tienes acceso para acceder a esta información");
            }
            var area = await _repository.GetByIdAsync(tenant.Value, id, ct);
            return _mapper.Map<AreaResponse>(area);

        }
    }
}
