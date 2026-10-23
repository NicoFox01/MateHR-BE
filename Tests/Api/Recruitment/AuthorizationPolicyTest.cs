using System.Security.Claims;
using FluentAssertions;
using MateHR.Api.Authorization;
using MateHR.Api.Tests.Common;
using MateHR.Domain.Common;
using MateHR.Domain.Users.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;


namespace MateHR.Tests.Api.Recruitment
{
    public class AuthorizationPolicyTest : IDisposable
    {
        private readonly CustomWebApplicationFactory _factory = new();
        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task InvokeAsync_RequireTenantAdmin_UseRecruiter_Returns403()
        {
            using var scope = _factory.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var claims = new[]
            {
                new Claim(AuthClaims.Role, UserRole.Recruiter.ToString())
            };

            var identity = new ClaimsIdentity(claims, authenticationType: "Test");
            var principal = new ClaimsPrincipal(identity);

            var result = await authService.AuthorizeAsync(principal, resource: null, policyName: AuthPolicies.RequireTenantAdmin);

            result.Succeeded.Should().BeFalse();
        }
        [Fact]
        public async Task InvokeAsync_RequireTenantAdmin_UseAdmin_Passes()
        {
            using var scope = _factory.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var claims = new[]
            {
        new Claim(AuthClaims.Role, UserRole.Admin.ToString())
        // Si RequireTenantAdmin valida la presencia del tenant:
        // new Claim(AuthClaims.TenantId, Guid.NewGuid().ToString())
    };

            var identity = new ClaimsIdentity(
                claims,
                authenticationType: "Test",
                nameType: ClaimTypes.Name,
                roleType: AuthClaims.Role);

            var principal = new ClaimsPrincipal(identity);

            var result = await authService.AuthorizeAsync(principal, resource: null, policyName: AuthPolicies.RequireTenantAdmin);

            var failureDetails = result.Failure != null
                ? string.Join(", ", result.Failure.FailedRequirements.Select(r => r.GetType().Name))
                : string.Empty;

            result.Succeeded.Should().BeTrue($"fallaron los siguientes requisitos: {failureDetails}");
        }
        [Fact]
        public async Task InvokeAsync_RequireTenantAdmin_UseSuperAdmin_Returns403()
        {
            using var scope = _factory.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var claims = new[]
            {
                new Claim(AuthClaims.Role, UserRole.SuperAdmin.ToString())
            };

            var identity = new ClaimsIdentity(claims, authenticationType: "Test");
            var principal = new ClaimsPrincipal(identity);

            var result = await authService.AuthorizeAsync(principal, resource: null, policyName: AuthPolicies.RequireTenantAdmin);

            result.Succeeded.Should().BeFalse();
        }
        [Fact]
        public async Task InvokeAsync_RequireRecruiter_UseRecruiter_Passes()
        {
            using var scope = _factory.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var claims = new[]
            {
                new Claim(AuthClaims.Role, UserRole.Recruiter.ToString())
                // Si tu política exige TenantId, descomenta:
                // new Claim(AuthClaims.TenantId, Guid.NewGuid().ToString())
            };

            // Al pasar AuthClaims.Role como el 'roleType', ClaimsPrincipal.IsInRole(...)
            // y RequireRole(...) reconocen correctamente el claim
            var identity = new ClaimsIdentity(
                claims,
                authenticationType: "Test",
                nameType: ClaimTypes.Name,
                roleType: AuthClaims.Role);

            var principal = new ClaimsPrincipal(identity);

            var result = await authService.AuthorizeAsync(principal, resource: null, policyName: AuthPolicies.RequireRecruiter);

            var failureDetails = result.Failure != null
                ? string.Join(", ", result.Failure.FailedRequirements.Select(r => r.GetType().Name))
                : string.Empty;

            result.Succeeded.Should().BeTrue($"fallaron los siguientes requisitos: {failureDetails}");
        }
        [Fact]
        public async Task InvokeAsync_RequireRecruiter_UseAdmin_Passes()
        {
            using var scope = _factory.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var claims = new[]
            {
                new Claim(AuthClaims.Role, UserRole.Admin.ToString())
                // Si tu política exige TenantId, descomenta:
                // new Claim(AuthClaims.TenantId, Guid.NewGuid().ToString())
            };

            // Al pasar AuthClaims.Role como el 'roleType', ClaimsPrincipal.IsInRole(...)
            // y RequireRole(...) reconocen correctamente el claim
            var identity = new ClaimsIdentity(
                claims,
                authenticationType: "Test",
                nameType: ClaimTypes.Name,
                roleType: AuthClaims.Role);

            var principal = new ClaimsPrincipal(identity);

            var result = await authService.AuthorizeAsync(principal, resource: null, policyName: AuthPolicies.RequireRecruiter);

            var failureDetails = result.Failure != null
                ? string.Join(", ", result.Failure.FailedRequirements.Select(r => r.GetType().Name))
                : string.Empty;

            result.Succeeded.Should().BeTrue($"fallaron los siguientes requisitos: {failureDetails}");
        }
        [Fact]
        public async Task InvokeAsync_RequireRecruiter_UseSuperAdmin_Returns403()
        {
            using var scope = _factory.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var claims = new[]
            {
                new Claim(AuthClaims.Role, UserRole.SuperAdmin.ToString())
            };

            var identity = new ClaimsIdentity(claims, authenticationType: "Test");
            var principal = new ClaimsPrincipal(identity);

            var result = await authService.AuthorizeAsync(principal, resource: null, policyName: AuthPolicies.RequireRecruiter);

            result.Succeeded.Should().BeFalse();
        }
        [Fact]
        public async Task InvokeAsync_RequireRecruiter_UseEmployee_Returns403()
        {
            using var scope = _factory.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

            var claims = new[]
            {
                new Claim(AuthClaims.Role, UserRole.Employee.ToString())
            };

            var identity = new ClaimsIdentity(claims, authenticationType: "Test");
            var principal = new ClaimsPrincipal(identity);

            var result = await authService.AuthorizeAsync(principal, resource: null, policyName: AuthPolicies.RequireRecruiter);

            result.Succeeded.Should().BeFalse();
        }

    }
}
