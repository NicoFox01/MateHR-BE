using AutoMapper;
using FluentValidation;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Interfaces;
using MateHR.Domain.Common;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Tenants.Interfaces;
using MateHR.Domain.Tenants.ValueObjects;

namespace MateHR.Application.Tenants.Commands
{
    public class UpdateTenant : IUpdateTenant
    {
        private readonly ITenantRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateTenantDto> _validator;

        public UpdateTenant(ITenantRepository repository, IMapper mapper, IValidator<UpdateTenantDto> validator)
        {
            _repository = repository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<TenantResponse> ExecuteAsync(Guid tenantId, UpdateTenantDto updateTenantDto, CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(updateTenantDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var tenantEntity = await _repository.GetTenantByIdAsync(tenantId, cancellationToken);

            var name = updateTenantDto.Name.Trim();
            var slug = SlugGenerator.Generate(name);

            if (await _repository.ExistsBySlugAsync(slug, tenantEntity.Id, cancellationToken))
            {
                throw new SlugAlreadyExistsException(slug);
            }

            tenantEntity.Update(
                name,
                slug,
                updateTenantDto.CUIT,
                updateTenantDto.OwnerEmail,
                updateTenantDto.Industry!.Value,
                _mapper.Map<Address?>(updateTenantDto.Address));

            var updatedTenant = await _repository.UpdateTenantAsync(tenantEntity, cancellationToken);

            return _mapper.Map<TenantResponse>(updatedTenant);
        }
    }
}