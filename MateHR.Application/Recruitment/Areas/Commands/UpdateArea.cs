using AutoMapper;
using FluentValidation;
using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Application.Recruitment.Areas.Interfaces;
using MateHR.Domain.Recruitment.Exceptions;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Recruitment.Areas.Commands
{
    public class UpdateArea : IUpdateArea
    {
        private readonly IAreaRepository _repository;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateAreaDto> _validator;

        public UpdateArea(
            IAreaRepository repository,
            ITenantContext tenantContext,
            IMapper mapper,
            IValidator<UpdateAreaDto> validator)
        {
            _repository = repository;
            _tenantContext = tenantContext;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<AreaResponse> ExecuteAsync(Guid id, UpdateAreaDto dto, CancellationToken ct = default)
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
            await _repository.GetByIdAsync(tenantId.Value, id, ct);
            var name = dto.Name.Trim();
            var areaExists = await _repository.NameExistsAsync(tenantId.Value, name, id, ct);
            if (areaExists)
            {
                throw new DuplicateEntityNameException(name);
            }
            var updated = await _repository.RenameAsync(tenantId.Value, id, name, ct);
            return _mapper.Map<AreaResponse>(updated);
        }
    }
}
