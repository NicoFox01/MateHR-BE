using System.Diagnostics;
using System.Globalization;
using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Common;

namespace MateHR.Api.Middleware
{
    /// <summary>
    /// Valida que el <c>SecurityStamp</c> del JWT siga siendo el vigente en la base.
    /// Un token emitido antes de un cambio de password, un deactivate o un activate
    /// deja de ser valido aunque no haya expirado.
    /// </summary>
    public class SecurityStampValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityStampValidationMiddleware> _logger;

        public SecurityStampValidationMiddleware(
            RequestDelegate next,
            ILogger<SecurityStampValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IUserSecurityStampValidator validator)
        {
            var user = context.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            var userId = ReadGuidClaim(user, AuthClaims.UserId);
            var stamp = ReadGuidClaim(user, AuthClaims.SecurityStamp);

            if (!userId.HasValue || !stamp.HasValue)
            {
                _logger.LogWarning("[{TraceId}] Token sin security_stamp valido en {Method} {Path}",
                    GetTraceId(context), context.Request.Method, context.Request.Path);

                await WriteUnauthorizedAsync(context, "La sesion no es valida.");
                return;
            }

            if (!await validator.IsStampCurrentAsync(userId.Value, stamp.Value, context.RequestAborted))
            {
                _logger.LogWarning("[{TraceId}] SecurityStamp obsoleto para el usuario {UserId} en {Method} {Path}",
                    GetTraceId(context), userId.Value, context.Request.Method, context.Request.Path);

                await WriteUnauthorizedAsync(context, "La sesion no es valida.");
                return;
            }

            await _next(context);
        }

        private static Guid? ReadGuidClaim(System.Security.Claims.ClaimsPrincipal user, string claimType)
        {
            var value = user.FindFirst(claimType)?.Value;

            if (!string.IsNullOrWhiteSpace(value)
                && Guid.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static async Task WriteUnauthorizedAsync(HttpContext context, string message)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsJsonAsync(new
            {
                message,
                traceId = GetTraceId(context)
            });
        }

        private static string GetTraceId(HttpContext context)
            => Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }
}