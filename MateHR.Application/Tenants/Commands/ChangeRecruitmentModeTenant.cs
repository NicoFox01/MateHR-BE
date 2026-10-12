using AutoMapper;
using FluentValidation;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Interfaces;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Tenants.Commands
{
    public class ChangeRecruitmentModeTenant : IChangeRecruitmentModeTenant
    {
        private readonly ITenantRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<ChangeRecruitmentModeDto> _validator;

        public ChangeRecruitmentModeTenant(ITenantRepository repository, IMapper mapper, IValidator<ChangeRecruitmentModeDto> validator)
        {
            _repository = repository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<TenantResponse> ExecuteAsync(Guid tenantId, ChangeRecruitmentModeDto changeRecruitmentModeDto, CancellationToken cancellationToken = default)
        {
            var validationResult = await _validator.ValidateAsync(changeRecruitmentModeDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var updatedTenant = await _repository.UpdateRecruitmentModeTenantAsync(tenantId, changeRecruitmentModeDto.RecruitmentMode, cancellationToken);

            return _mapper.Map<TenantResponse>(updatedTenant);
        }
    }
}