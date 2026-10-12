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
        private readonly IGetUsers _getUsers;
        private readonly IChangeUserStatus _changeUserStatus;

        public UsersController(
            ICreateUser createUser,
            IGetUsers getUsers,
            IChangeUserStatus changeUserStatus)
        {
            _createUser = createUser;
            _getUsers = getUsers;
            _changeUserStatus = changeUserStatus;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(
            [FromQuery] GetUsersRequest request,
            CancellationToken cancellationToken)
        {
            var users = await _getUsers.ExecuteAsync(request, cancellationToken);

            return Ok(users);
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

        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponse>> ChangeStatus(
            Guid id,
            [FromBody] ChangeUserStatusDto request,
            CancellationToken cancellationToken)
        {
            var user = await _changeUserStatus.ExecuteAsync(id, request, cancellationToken);

            return Ok(user);
        }
    }
}