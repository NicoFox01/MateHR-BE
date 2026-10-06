using MateHR.Api.Authorization;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Interfaces;
using MateHR.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MateHR.Api.Controllers.v1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [Authorize(Policy = AuthPolicies.RequireSuperAdmin)]
    public class TenantsController : ControllerBase
    {
        private readonly ICreateTenant _createTenant;
        private readonly IUpdateTenant _updateTenant;
        private readonly IChangeStatusTenant _changeStatusTenant;
        private readonly IChangeRecruitmentModeTenant _changeRecruitmentModeTenant;
        private readonly IChangeSubscriptionTypeTenant _changeSubscriptionTypeTenant;
        private readonly IGetTenantById _getTenantById;
        private readonly IGetTenantBySlug _getTenantBySlug;
        private readonly IGetTenants _getTenants;

        public TenantsController(
            ICreateTenant createTenant,
            IUpdateTenant updateTenant,
            IChangeStatusTenant changeStatusTenant,
            IChangeRecruitmentModeTenant changeRecruitmentModeTenant,
            IChangeSubscriptionTypeTenant changeSubscriptionTypeTenant,
            IGetTenantById getTenantById,
            IGetTenantBySlug getTenantBySlug,
            IGetTenants getTenants)
        {
            _createTenant = createTenant;
            _updateTenant = updateTenant;
            _changeStatusTenant = changeStatusTenant;
            _changeRecruitmentModeTenant = changeRecruitmentModeTenant;
            _changeSubscriptionTypeTenant = changeSubscriptionTypeTenant;
            _getTenantById = getTenantById;
            _getTenantBySlug = getTenantBySlug;
            _getTenants = getTenants;
        }

        [HttpPost]
        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TenantResponse>> Create(
            [FromBody] CreateTenantDto request,
            CancellationToken cancellationToken)
        {
            var tenant = await _createTenant.ExecuteAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
        }

        [HttpGet]

        [ProducesResponseType(typeof(PagedResult<TenantResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<TenantResponse>>> GetAll(
            [FromQuery] GetTenantsRequest request,
            CancellationToken cancellationToken)
        {
            var tenants = await _getTenants.ExecuteAsync(request, cancellationToken);

            return Ok(tenants);
        }

        [HttpGet("{id:guid}")]

        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TenantResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var tenant = await _getTenantById.ExecuteAsync(id, cancellationToken);

            return Ok(tenant);
        }

        [HttpGet("slug/{slug}")]
        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TenantResponse>> GetBySlug(
            string slug,
            CancellationToken cancellationToken)
        {
            var tenant = await _getTenantBySlug.ExecuteAsync(slug, cancellationToken);

            return Ok(tenant);
        }

        [HttpPut("{id:guid}")]

        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TenantResponse>> Update(
            Guid id,
            [FromBody] UpdateTenantDto request,
            CancellationToken cancellationToken)
        {
            var tenant = await _updateTenant.ExecuteAsync(id, request, cancellationToken);

            return Ok(tenant);
        }

        [HttpPatch("{id:guid}/status")]

        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TenantResponse>> ChangeStatus(
            Guid id,
            [FromBody] ChangeTenantStatusDto request,
            CancellationToken cancellationToken)
        {
            var tenant = await _changeStatusTenant.ExecuteAsync(id, request, cancellationToken);

            return Ok(tenant);
        }

        [HttpPatch("{id:guid}/recruitment-mode")]

        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TenantResponse>> ChangeRecruitmentMode(
            Guid id,
            [FromBody] ChangeRecruitmentModeDto request,
            CancellationToken cancellationToken)
        {
            var tenant = await _changeRecruitmentModeTenant.ExecuteAsync(id, request, cancellationToken);

            return Ok(tenant);
        }

        [HttpPatch("{id:guid}/subscription-type")]

        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TenantResponse>> ChangeSubscriptionType(
            Guid id,
            [FromBody] ChangeSubscriptionTypeDto request,
            CancellationToken cancellationToken)
        {
            var tenant = await _changeSubscriptionTypeTenant.ExecuteAsync(id, request, cancellationToken);

            return Ok(tenant);
        }
    }
}