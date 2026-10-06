using AutoMapper;
using FluentValidation;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Interfaces;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Tenants.Commands
{
    public class ChangeStatusTenant : IChangeStatusTenant
    {
        private readonly ITenantRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<ChangeTenantStatusDto> _validator;

        public ChangeStatusTenant(ITenantRepository repository, IMapper mapper, IValidator<ChangeTenantStatusDto> validator)
        {
            _repository = repository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<TenantResponse> ExecuteAsync(Guid tenantId, ChangeTenantStatusDto changeTenantStatusDto, CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(changeTenantStatusDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var updatedTenant = await _repository.UpdateStatusTenantAsync(tenantId, changeTenantStatusDto.Status, cancellationToken);

            return _mapper.Map<TenantResponse>(updatedTenant);
        }
    }
}