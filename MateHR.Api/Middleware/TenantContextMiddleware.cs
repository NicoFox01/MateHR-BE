using MateHR.Domain.Common;
using MateHR.Domain.Tenants.Interfaces;
using MateHR.Infrastructure.Services;

namespace MateHR.Api.Middleware
{
    public class TenantContextMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
        {
            if (tenantContext is TenantContext mutableContext)
            {
                mutableContext.SetCurrentTenantId(ResolveTenantId(context));
            }

            await _next(context);
        }

        private static Guid? ResolveTenantId(HttpContext context)
        {
            var claim = context.User?.FindFirst(AuthClaims.TenantId);

            if (claim is null)
            {
                return null;
            }

            return Guid.TryParse(claim.Value, out var tenantId) && tenantId != Guid.Empty
                ? tenantId
                : null;
        }
    }
}