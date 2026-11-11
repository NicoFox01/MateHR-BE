using MateHR.Api.Authorization;
using MateHR.Application.Recruitment.JobStatuses.DTOs;
using MateHR.Application.Recruitment.JobStatuses.Interfaces;
using MateHR.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MateHR.Api.Controllers.v1.Recruitment
{
    [ApiController]
    [Route("api/v1/recruitment/job-statuses")]
    [Produces("application/json")]
    [Authorize(Policy = AuthPolicies.RequireRecruiter)]
    public class JobStatusesController : ControllerBase
    {
        private readonly ICreateJobStatus _createJobStatus;
        private readonly IUpdateJobStatus _updateJobStatus;
        private readonly IDeleteJobStatus _deleteJobStatus;
        private readonly IGetJobStatusById _getJobStatusById;
        private readonly IGetJobStatuses _getJobStatuses;
        public JobStatusesController(
            ICreateJobStatus createJobStatus,
            IUpdateJobStatus updateJobStatus,
            IDeleteJobStatus deleteJobStatus,
            IGetJobStatusById getJobStatusById,
            IGetJobStatuses getJobStatuses
            )
        {
            _createJobStatus = createJobStatus;
            _updateJobStatus = updateJobStatus;
            _deleteJobStatus = deleteJobStatus;
            _getJobStatusById = getJobStatusById;
            _getJobStatuses = getJobStatuses;
        }
        [Authorize(Policy = AuthPolicies.RequireTenantAdmin)]
        [HttpPost]
        [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<JobStatusResponse>> Create(
            [FromBody] CreateJobStatusDto request,
            CancellationToken cancellationToken)
        {
            var jobStatus = await _createJobStatus.ExecuteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = jobStatus.Id }, jobStatus);
        }
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JobStatusResponse>> GetById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var jobStatus = await _getJobStatusById.ExecuteAsync(id, cancellationToken);
            return Ok(jobStatus);
        }
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<JobStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<JobStatusResponse>>> GetAll(
            [FromQuery] GetJobStatusesRequest request,
            CancellationToken cancellationToken)
        {
            var jobStatuses = await _getJobStatuses.ExecuteAsync(request, cancellationToken);
            return Ok(jobStatuses);
        }
        [Authorize(Policy = AuthPolicies.RequireTenantAdmin)]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<JobStatusResponse>> Update(
            [FromRoute] Guid id,
            [FromBody] UpdateJobStatusDto request,
            CancellationToken cancellationToken)
        {
            var jobStatus = await _updateJobStatus.ExecuteAsync(id, request, cancellationToken);
            return Ok(jobStatus);
        }
        [Authorize(Policy = AuthPolicies.RequireTenantAdmin)]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            await _deleteJobStatus.ExecuteAsync(id, cancellationToken);
            return NoContent();
        }

    }
}
