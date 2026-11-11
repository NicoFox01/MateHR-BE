using AutoMapper;
using FluentValidation;
using MateHR.Application.Recruitment.JobStatuses.DTOs;
using MateHR.Application.Recruitment.JobStatuses.Interfaces;
using MateHR.Domain.Common;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Application.Recruitment.JobStatuses.Queries
{
    public class GetJobStatuses : IGetJobStatuses
    {
        private readonly IJobStatusRepository _repository;
        private readonly ITenantContext _tenantContext;
        private readonly IMapper _mapper;
        private readonly IValidator<GetJobStatusesRequest> _validator;
        public GetJobStatuses(
            IJobStatusRepository repository,
            ITenantContext tenantContext,
            IMapper mapper,
            IValidator<GetJobStatusesRequest> validator
            )
        {
            _repository = repository;
            _tenantContext = tenantContext;
            _mapper = mapper;
            _validator = validator;
        }
        public async Task<PagedResult<JobStatusResponse>> ExecuteAsync(GetJobStatusesRequest request, CancellationToken ct = default)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var tenantId = _tenantContext.CurrentTenantId;

            if (tenantId == null)
            {
                throw new UnauthorizedAccessException("No tienes acceso para acceder a esta información");
            }
            var jobStatusesPaged = await _repository.GetAllAsync(tenantId.Value, request.PageNumber, request.PageSize, ct);
            return _mapper.Map<PagedResult<JobStatusResponse>>(jobStatusesPaged);
        }
    }
}
