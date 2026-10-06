using MateHR.Api.Authorization;
using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MateHR.Api.Controllers.v1
{
    [ApiController]
    [Route("api/v1/users")]
    [Produces("application/json")]
    [Authorize(Policy = AuthPolicies.RequireSuperAdmin)]
    public class UsersController : ControllerBase
    {
        private readonly ICreateUser _createUser;

        public UsersController(ICreateUser createUser)
        {
            _createUser = createUser;
        }

        [HttpPost]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserResponse>> Create(
            [FromBody] CreateUserRequest request,
            CancellationToken cancellationToken)
        {
            var created = await _createUser.ExecuteAsync(request, cancellationToken);

            return Created($"/api/v1/auth/me", created);
        }
    }
}