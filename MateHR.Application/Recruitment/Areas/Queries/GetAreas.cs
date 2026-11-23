using AutoMapper;
using FluentValidation;
using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Application.Recruitment.Areas.Interfaces;
using MateHR.Domain.Common;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Recruitment.Areas.Queries
{
    public class GetAreas : IGetAreas
    {
        private readonly IAreaRepository _repository;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;
        private readonly IValidator<GetAreasRequest> _validator;

        public GetAreas(
            IAreaRepository repository,
            ITenantContext tenantContext,
            IMapper mapper,
            IValidator<GetAreasRequest> validator)
        {
            _repository = repository;
            _tenantContext = tenantContext;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<PagedResult<AreaResponse>> ExecuteAsync(GetAreasRequest request, CancellationToken ct = default)
        {
            var validationResult = await _validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
            var tenant = _tenantContext.CurrentTenantId;
            if(tenant == null)
            {
                throw new UnauthorizedAccessException("No tienes acceso para acceder a esta información");
            }
            var paged = await _repository.GetAllAsync(tenant.Value, request.PageNumber, request.PageSize, request.IncludeInactive, ct);
            return _mapper.Map<PagedResult<AreaResponse>>(paged);
        }
    }
}
