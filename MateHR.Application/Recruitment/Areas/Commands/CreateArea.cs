using AutoMapper;
using FluentValidation;
using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Application.Recruitment.Areas.Interfaces;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Exceptions;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Recruitment.Areas.Commands
{
    public class CreateArea : ICreateArea
    {
        private readonly IAreaRepository _repository;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateAreaDto> _validator;

        public CreateArea(
            IAreaRepository repository,
            ITenantContext tenantContext,
            IMapper mapper,
            IValidator<CreateAreaDto> validator)
        {
            _repository = repository;
            _tenantContext = tenantContext;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<AreaResponse> ExecuteAsync(CreateAreaDto dto, CancellationToken ct = default)
        {
            var validationResult = await _validator.ValidateAsync(dto, ct);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
            var tenantId = _tenantContext.CurrentTenantId;
            if(tenantId == null)
            {
                throw new UnauthorizedAccessException("No tienes acceso para acceder a esta información");
            }
            var name = dto.Name.Trim();
            var exists = await _repository.NameExistsAsync(tenantId.Value, name, null, ct);
            if (exists)
            {
                throw new DuplicateEntityNameException(name);
            }
            var area = new Area(tenantId.Value, name);
            var created = await _repository.CreateAsync(area, ct);
            return _mapper.Map<AreaResponse>(created);
        }
    }
}
