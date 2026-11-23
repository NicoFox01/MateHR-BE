using MateHR.Api.Authorization;
using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Application.Recruitment.Areas.Interfaces;
using MateHR.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MateHR.Api.Controllers.v1.Recruitment
{
        [ApiController]
    [Route("api/v1/recruitment/areas")]
    [Produces("application/json")]
    [Authorize(Policy = AuthPolicies.RequireRecruiter)]
    public class AreasController : ControllerBase
    {
        private readonly ICreateArea _createArea;
        private readonly IUpdateArea _updateArea;
        private readonly IDeleteArea _deleteArea;
        private readonly IGetAreaById _getAreaById;
        private readonly IGetAreas _getAreas;
        private readonly IActivateArea _activateArea;
        private readonly IDeactivateArea _deactivateArea;
        public AreasController(
            ICreateArea createArea,
            IUpdateArea updateArea,
            IDeleteArea deleteArea,
            IGetAreaById getAreaById,
            IGetAreas getAreas,
            IActivateArea activateArea,
            IDeactivateArea deactivateArea
            )
        {
            _createArea = createArea;
            _updateArea = updateArea;
            _deleteArea = deleteArea;
            _getAreaById = getAreaById;
            _getAreas = getAreas;
            _activateArea = activateArea;
            _deactivateArea = deactivateArea;
        }
        [Authorize(Policy = AuthPolicies.RequireTenantAdmin)]
        [HttpPost]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AreaResponse>> Create(
            [FromBody] CreateAreaDto request,
            CancellationToken cancellationToken)
        {
            var area = await _createArea.ExecuteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = area.Id }, area);
        }
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AreaResponse>> GetById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken
            )
        {
            var area = await _getAreaById.ExecuteAsync(id, cancellationToken);
            if (!area.IsActive && !User.IsInRole(AuthPolicies.AdminRole))
            {
                return NotFound();
            }
            return Ok(area);
        }
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<AreaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<AreaResponse>>> GetAll(
            [FromQuery] GetAreasRequest request,
            CancellationToken cancellationToken)
        {
            if (!User.IsInRole(AuthPolicies.AdminRole))
            {
                request.IncludeInactive = false;
            }
            var areas = await _getAreas.ExecuteAsync(request, cancellationToken);
            return Ok(areas);
        }
        [Authorize(Policy = AuthPolicies.RequireTenantAdmin)]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AreaResponse>> Update(
            [FromRoute] Guid id,
            [FromBody] UpdateAreaDto request,
            CancellationToken cancellationToken)
        {
            var area = await _updateArea.ExecuteAsync(id, request, cancellationToken);
            return Ok(area);
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
            await _deleteArea.ExecuteAsync(id, cancellationToken);
            return NoContent();
        }
        [Authorize(Policy = AuthPolicies.RequireTenantAdmin)]
        [HttpPatch("{id:guid}/activate")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AreaResponse>> Activate(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var area = await _activateArea.ExecuteAsync(id, cancellationToken);
            return Ok(area);
        }
        [Authorize(Policy = AuthPolicies.RequireTenantAdmin)]
        [HttpPatch("{id:guid}/deactivate")]
        [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AreaResponse>> Deactivate(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            var area = await _deactivateArea.ExecuteAsync(id, cancellationToken);
            return Ok(area);
        }
    }
}
