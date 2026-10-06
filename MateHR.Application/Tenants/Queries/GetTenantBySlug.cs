using AutoMapper;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Interfaces;
using MateHR.Domain.Common;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Tenants.Queries
{
    public class GetTenantBySlug : IGetTenantBySlug
    {
        private readonly ITenantRepository _repository;
        private readonly IMapper _mapper;

        public GetTenantBySlug(ITenantRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<TenantResponse> ExecuteAsync(string slug, CancellationToken cancellationToken = default)
        {
            var normalizedSlug = SlugGenerator.Generate(slug);
            var tenant = await _repository.GetTenantBySlugAsync(normalizedSlug, cancellationToken);

            return _mapper.Map<TenantResponse>(tenant);
        }
    }
}