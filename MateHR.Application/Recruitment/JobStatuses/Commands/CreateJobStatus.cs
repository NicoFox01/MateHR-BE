using AutoMapper;
using FluentValidation;
using MateHR.Application.Recruitment.JobStatuses.DTOs;
using MateHR.Application.Recruitment.JobStatuses.Interfaces;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Exceptions;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Application.Recruitment.JobStatuses.Commands
{
    public class CreateJobStatus : ICreateJobStatus
    {
        private readonly IJobStatusRepository _repository;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateJobStatusDto> _validator;
        public CreateJobStatus(
            IJobStatusRepository repository,
            ITenantContext tenantContext,
            IMapper mapper,
            IValidator<CreateJobStatusDto> validator
            )
        {
            _repository = repository;
            _tenantContext = tenantContext;
            _mapper = mapper;
            _validator = validator;
        }
        public async Task<JobStatusResponse> ExecuteAsync(CreateJobStatusDto dto, CancellationToken ct = default)
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
            var name = dto.Name.Trim();
            var jobStatusExist = await _repository.NameExistsAsync(tenantId.Value, name, null, ct);
            if (jobStatusExist)
            {
                throw new DuplicateEntityNameException(name);
            }
            var JobStatusEntity = new JobStatus(
                tenantId.Value,
                name
            );
            var created = await _repository.CreateAsync(JobStatusEntity, ct);
            return _mapper.Map<JobStatusResponse>(created);
        }
    }
}
