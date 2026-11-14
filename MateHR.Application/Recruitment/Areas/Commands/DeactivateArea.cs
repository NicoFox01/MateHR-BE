using AutoMapper;
using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Application.Recruitment.Areas.Interfaces;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Recruitment.Areas.Commands
{
    public class DeactivateArea : IDeactivateArea
    {
        private readonly IAreaRepository _repository;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;

        public DeactivateArea(
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
            var updated = await _repository.SetActiveAsync(tenant.Value, id, isActive: false, ct);
            return _mapper.Map<AreaResponse>(updated);
        }
    }
}
