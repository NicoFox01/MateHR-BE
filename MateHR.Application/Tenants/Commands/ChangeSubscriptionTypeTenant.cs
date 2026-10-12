using AutoMapper;
using FluentValidation;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Interfaces;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Tenants.Commands
{
    public class ChangeSubscriptionTypeTenant : IChangeSubscriptionTypeTenant
    {
        private readonly ITenantRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<ChangeSubscriptionTypeDto> _validator;

        public ChangeSubscriptionTypeTenant(ITenantRepository repository, IMapper mapper, IValidator<ChangeSubscriptionTypeDto> validator)
        {
            _repository = repository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<TenantResponse> ExecuteAsync(Guid tenantId, ChangeSubscriptionTypeDto changeSubscriptionTypeDto, CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(changeSubscriptionTypeDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var updatedTenant = await _repository.UpdateSubscriptionTypeTenantAsync(tenantId, changeSubscriptionTypeDto.SubscriptionType, cancellationToken);

            return _mapper.Map<TenantResponse>(updatedTenant);
        }
    }
}