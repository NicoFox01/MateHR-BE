using AutoMapper;
using FluentValidation;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Interfaces;
using MateHR.Domain.Common;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Tenants.Queries
{
    public class GetTenants : IGetTenants
    {
        private readonly ITenantRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<GetTenantsRequest> _validator;

        public GetTenants(ITenantRepository repository, IMapper mapper, IValidator<GetTenantsRequest> validator)
        {
            _repository = repository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<PagedResult<TenantResponse>> ExecuteAsync(GetTenantsRequest request, CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var tenants = await _repository.GetTenantsAsync(request.Status, request.PageNumber, request.PageSize, cancellationToken);

            return _mapper.Map<PagedResult<TenantResponse>>(tenants);
        }
    }
}