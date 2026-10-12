using AutoMapper;
using FluentValidation;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Interfaces;
using MateHR.Domain.Common;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.Interfaces;
using MateHR.Domain.Tenants.ValueObjects;

namespace MateHR.Application.Tenants.Commands
{
    public class CreateTenant : ICreateTenant
    {
        private readonly ITenantRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateTenantDto> _validator;

        public CreateTenant(ITenantRepository repository, IMapper mapper, IValidator<CreateTenantDto> validator)
        {
            _repository = repository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<TenantResponse> ExecuteAsync(CreateTenantDto createTenantDto, CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(createTenantDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var name = createTenantDto.Name.Trim();
            var slug = SlugGenerator.Generate(name);

            if (await _repository.ExistsBySlugAsync(slug, null, cancellationToken))
            {
                throw new SlugAlreadyExistsException(slug);
            }

            var tenantEntity = new Tenant(
                name,
                slug,
                createTenantDto.CUIT,
                createTenantDto.OwnerEmail,
                createTenantDto.Industry!.Value,
                _mapper.Map<Address?>(createTenantDto.Address),
                TenantStatus.Active,
                RecruitmentMode.Internal,
                SubscriptionType.Free);

            var tenant = await _repository.CreateTenantAsync(tenantEntity, cancellationToken);

            return _mapper.Map<TenantResponse>(tenant);
        }
    }
}