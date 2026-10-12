using AutoMapper;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Interfaces;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Tenants.Queries
{
    public class GetTenantById : IGetTenantById
    {
        private readonly ITenantRepository _repository;
        private readonly IMapper _mapper;

        public GetTenantById(ITenantRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<TenantResponse> ExecuteAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            var tenant = await _repository.GetTenantByIdAsync(tenantId, cancellationToken);

            return _mapper.Map<TenantResponse>(tenant);
        }
    }
}