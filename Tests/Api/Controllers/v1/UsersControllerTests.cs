using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MateHR.Api.Tests.Common;
using MateHR.Domain.Users.Enums;
using Xunit;

namespace MateHR.Api.Tests.Controllers.v1
{
    public class UsersControllerTests : IDisposable
    {
        private const string UsersPath = "/api/v1/users";

        private readonly CustomWebApplicationFactory _factory = new();

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task Create_WithoutToken_Returns401()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientWithoutCookies();

            var response = await client.PostAsJsonAsync(UsersPath, new
            {
                firstName = "Ana",
                lastName = "Pérez",
                email = "ana@acme.com",
                password = "secreta123",
                role = "Recruiter",
                tenantId = tenant.Id
            });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Create_WithAdminToken_Returns403()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin);

            var response = await client.PostAsJsonAsync(UsersPath, new
            {
                firstName = "Ana",
                lastName = "Pérez",
                email = "ana@acme.com",
                password = "secreta123",
                role = "Recruiter",
                tenantId = tenant.Id
            });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_SuperAdminValidUser_Returns201WithUserResponse()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PostAsJsonAsync(UsersPath, new
            {
                firstName = "Ana",
                lastName = "Pérez",
                email = "ana@acme.com",
                password = "secreta123",
                role = "Recruiter",
                tenantId = tenant.Id
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location!.ToString().Should().Contain("/api/v1/auth/me");
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("email").GetString().Should().Be("ana@acme.com");
            body.GetProperty("role").GetString().Should().Be("Recruiter");
        }

        [Fact]
        public async Task Create_DuplicateEmail_Returns409()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var body = new
            {
                firstName = "Ana",
                lastName = "Pérez",
                email = "dup@acme.com",
                password = "secreta123",
                role = "Recruiter",
                tenantId = tenant.Id
            };

            var first = await client.PostAsJsonAsync(UsersPath, body);
            first.StatusCode.Should().Be(HttpStatusCode.Created);

            var second = await client.PostAsJsonAsync(UsersPath, body);

            second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Create_InvalidDto_Returns400WithErrors()
        {
            _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PostAsJsonAsync(UsersPath, new
            {
                firstName = "Ana",
                lastName = "Pérez",
                email = "no-es-un-mail",
                password = "123",
                role = "Recruiter",
                tenantId = Guid.NewGuid()
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Object);
        }

        [Fact]
        public async Task Create_SuperAdminWithTenantId_Returns400()
        {
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PostAsJsonAsync(UsersPath, new
            {
                firstName = "Root",
                lastName = "Admin",
                email = "root@acme.com",
                password = "secreta123",
                role = "SuperAdmin",
                tenantId = Guid.NewGuid()
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Object);
        }

        [Fact]
        public async Task GetAll_WithoutToken_Returns401()
        {
            var client = _factory.CreateClientWithoutCookies();

            var response = await client.GetAsync(UsersPath);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAll_WithAdminToken_Returns403()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.GetAsync($"{UsersPath}?tenantId={tenant.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetAll_FilteredByTenantAndRole_ReturnsOnlyMatchingUsers()
        {
            var tenant = _factory.SeedTenant();
            _factory.SeedUser("admin1@acme.com", "secreta123", UserRole.Admin, tenant.Id);
            _factory.SeedUser("admin2@acme.com", "secreta123", UserRole.Admin, tenant.Id);
            _factory.SeedUser("recruiter@acme.com", "secreta123", UserRole.Recruiter, tenant.Id);
            var otherTenant = _factory.SeedTenant("Other", "other");

            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.GetAsync(
                $"{UsersPath}?tenantId={tenant.Id}&role=Admin");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetArrayLength().Should().Be(2);
            body[0].GetProperty("role").GetString().Should().Be("Admin");
        }

        [Fact]
        public async Task ChangeStatus_Deactivate_Returns200WithIsActiveFalse()
        {
            var tenant = _factory.SeedTenant();
            var admin = _factory.SeedUser("admin@acme.com", "secreta123", UserRole.Admin, tenant.Id);
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PatchAsJsonAsync(
                $"{UsersPath}/{admin.Id}/status",
                new { isActive = false });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("isActive").GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task ChangeStatus_Reactivate_Returns200WithIsActiveTrue()
        {
            var tenant = _factory.SeedTenant();
            var admin = _factory.SeedUser("admin@acme.com", "secreta123", UserRole.Admin, tenant.Id);
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            await client.PatchAsJsonAsync($"{UsersPath}/{admin.Id}/status", new { isActive = false });

            var response = await client.PatchAsJsonAsync(
                $"{UsersPath}/{admin.Id}/status",
                new { isActive = true });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("isActive").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task ChangeStatus_UnknownUser_Returns404()
        {
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PatchAsJsonAsync(
                $"{UsersPath}/{Guid.NewGuid()}/status",
                new { isActive = false });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
