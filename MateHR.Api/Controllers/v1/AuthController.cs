using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Common;
using MateHR.Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MateHR.Api.Controllers.v1
{
    [ApiController]
    [Route("api/v1/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        public const string RefreshTokenCookieName = "matehr_refresh_token";

        private readonly ILoginUser _loginUser;
        private readonly ILogoutUser _logoutUser;
        private readonly IGetCurrentUser _getCurrentUser;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            ILoginUser loginUser,
            ILogoutUser logoutUser,
            IGetCurrentUser getCurrentUser,
            IOptions<JwtSettings> jwtSettings)
        {
            _loginUser = loginUser;
            _logoutUser = logoutUser;
            _getCurrentUser = getCurrentUser;
            _jwtSettings = jwtSettings.Value;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _loginUser.ExecuteAsync(request, cancellationToken);

            Response.Cookies.Append(
                RefreshTokenCookieName,
                response.RefreshToken,
                BuildCookieOptions(response.RefreshTokenExpiresAt));

            return Ok(response);
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[RefreshTokenCookieName];

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _logoutUser.ExecuteAsync(refreshToken, cancellationToken);
            }

            Response.Cookies.Delete(
                RefreshTokenCookieName,
                BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)));

            return NoContent();
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
        {
            var user = await _getCurrentUser.ExecuteAsync(ResolveUserId(), cancellationToken);

            return Ok(user);
        }

        private CookieOptions BuildCookieOptions(DateTimeOffset expiresAt)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
                Path = "/api/v1/auth"
            };
        }

        private Guid ResolveUserId()
        {
            var claim = User.FindFirst(AuthClaims.UserId);

            if (claim is null || !Guid.TryParse(claim.Value, out var userId))
            {
                throw new InvalidOperationException("El token no contiene un identificador de usuario valido.");
            }

            return userId;
        }
    }
}