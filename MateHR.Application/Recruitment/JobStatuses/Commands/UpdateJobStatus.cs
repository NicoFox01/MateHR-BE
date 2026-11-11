using AutoMapper;
using FluentValidation;
using MateHR.Application.Recruitment.JobStatuses.DTOs;
using MateHR.Application.Recruitment.JobStatuses.Interfaces;
using MateHR.Domain.Recruitment.Exceptions;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Application.Recruitment.JobStatuses.Commands
{
    public class UpdateJobStatus : IUpdateJobStatus
    {
        private readonly IJobStatusRepository _repository;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateJobStatusDto> _validator;
        public UpdateJobStatus(
            IJobStatusRepository repository,
            ITenantContext tenantContext,
            IMapper mapper,
            IValidator<UpdateJobStatusDto> validator
            )
        {
            _repository = repository;
            _tenantContext = tenantContext;
            _mapper = mapper;
            _validator = validator;
        }
        public async Task<JobStatusResponse> ExecuteAsync(Guid id, UpdateJobStatusDto dto, CancellationToken ct = default)
        {
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
            var tenantId = _tenantContext.CurrentTenantId;
            if (tenantId == null)
            {
                throw new UnauthorizedAccessException("No tienes acceso para acceder a esta información");
            }
            var JobStatusEntity = await _repository.GetByIdAsync(tenantId.Value, id, ct);
            var name = dto.Name.Trim();
            var jobStatusExist = await _repository.NameExistsAsync(tenantId.Value, name, id, ct);
            if (jobStatusExist)
            {
                throw new DuplicateEntityNameException(name);
            }
            var updated = await _repository.RenameAsync(tenantId.Value, id, name, ct);
            return _mapper.Map<JobStatusResponse>(updated);
        }
    }
}
