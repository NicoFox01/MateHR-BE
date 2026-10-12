using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MateHR.Api.Tests.Common;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Users.Enums;
using Xunit;

namespace MateHR.Api.Tests.Controllers.v1
{
    public class TenantsControllerTests : IDisposable
    {
        private const string TenantsPath = "/api/v1/tenants";

        private readonly CustomWebApplicationFactory _factory = new();

        public void Dispose() => _factory.Dispose();

        private static object ValidCreateBody(string name = "Acme Corp") => new
        {
            name,
            cuit = "20304050601",
            ownerEmail = "owner@acme.com",
            industry = "Technology"
        };

        [Fact]
        public async Task Endpoints_WithoutToken_Returns401()
        {
            var client = _factory.CreateClientWithoutCookies();

            var response = await client.PostAsJsonAsync(TenantsPath, ValidCreateBody());

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Endpoints_WithAdminToken_Returns403()
        {
            var client = _factory.CreateClientAs(UserRole.Admin);

            var response = await client.PostAsJsonAsync(TenantsPath, ValidCreateBody());

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_WithSuperAdminToken_Returns201WithLocation()
        {
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PostAsJsonAsync(TenantsPath, ValidCreateBody());

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var id = body.GetProperty("id").GetGuid();
            response.Headers.Location!.ToString().Should().ContainEquivalentOf($"/api/v1/tenants/{id}");
            body.GetProperty("slug").GetString().Should().Be("acme-corp");
        }

        [Fact]
        public async Task Create_DuplicateSlug_Returns409()
        {
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var first = await client.PostAsJsonAsync(TenantsPath, ValidCreateBody());
            first.StatusCode.Should().Be(HttpStatusCode.Created);

            var second = await client.PostAsJsonAsync(TenantsPath, ValidCreateBody());

            second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Create_InvalidDto_Returns400WithErrors()
        {
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PostAsJsonAsync(TenantsPath, new
            {
                name = "",
                cuit = "abc",
                ownerEmail = "no-es-un-mail",
                industry = "Technology"
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Object);
        }

        [Fact]
        public async Task GetAll_WithStatusFilter_Returns200WithPagedResult()
        {
            _factory.SeedTenant("Active Tenant", "active-tenant");
            _factory.SeedTenant("Suspended Tenant", "suspended-tenant", TenantStatus.Suspended);
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.GetAsync($"{TenantsPath}?status=Active");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var items = body.GetProperty("items");
            items.GetArrayLength().Should().Be(1);
            items[0].GetProperty("slug").GetString().Should().Be("active-tenant");
            body.GetProperty("totalCount").GetInt32().Should().Be(1);
        }

        [Fact]
        public async Task GetById_ExistingTenant_Returns200()
        {
            var tenant = _factory.SeedTenant("Get By Id", "get-by-id");
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.GetAsync($"{TenantsPath}/{tenant.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("id").GetGuid().Should().Be(tenant.Id);
        }

        [Fact]
        public async Task GetById_UnknownTenant_Returns404()
        {
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.GetAsync($"{TenantsPath}/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetBySlug_ExistingSlug_Returns200()
        {
            var tenant = _factory.SeedTenant("By Slug", "by-slug");
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.GetAsync($"{TenantsPath}/slug/{tenant.Slug}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("slug").GetString().Should().Be("by-slug");
        }

        [Fact]
        public async Task GetBySlug_UnknownSlug_Returns404()
        {
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.GetAsync($"{TenantsPath}/slug/no-existe");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_ExistingTenant_Returns200()
        {
            var tenant = _factory.SeedTenant("Old Name", "old-name");
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PutAsJsonAsync($"{TenantsPath}/{tenant.Id}", new
            {
                name = "New Name",
                cuit = "20304050601",
                ownerEmail = "nuevo@acme.com",
                industry = "Healthcare"
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("name").GetString().Should().Be("New Name");
            body.GetProperty("slug").GetString().Should().Be("new-name");
            body.GetProperty("ownerEmail").GetString().Should().Be("nuevo@acme.com");
        }

        [Fact]
        public async Task Update_DuplicateSlugOfAnotherTenant_Returns409()
        {
            _factory.SeedTenant("First Tenant", "first-tenant");
            var second = _factory.SeedTenant("Second Tenant", "second-tenant");
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PutAsJsonAsync($"{TenantsPath}/{second.Id}", new
            {
                name = "First Tenant",
                cuit = "20304050601",
                ownerEmail = "owner@acme.com",
                industry = "Technology"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task ChangeStatus_WithSuperAdminToken_Returns200()
        {
            var tenant = _factory.SeedTenant("Status Tenant", "status-tenant");
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PatchAsJsonAsync(
                $"{TenantsPath}/{tenant.Id}/status",
                new { status = "Suspended" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("status").GetString().Should().Be("Suspended");
        }

        [Fact]
        public async Task ChangeRecruitmentMode_WithSuperAdminToken_Returns200()
        {
            var tenant = _factory.SeedTenant("Mode Tenant", "mode-tenant");
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PatchAsJsonAsync(
                $"{TenantsPath}/{tenant.Id}/recruitment-mode",
                new { recruitmentMode = "External" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("recruitmentMode").GetString().Should().Be("External");
        }

        [Fact]
        public async Task ChangeSubscriptionType_WithSuperAdminToken_Returns200()
        {
            var tenant = _factory.SeedTenant("Sub Tenant", "sub-tenant");
            var client = _factory.CreateClientAs(UserRole.SuperAdmin);

            var response = await client.PatchAsJsonAsync(
                $"{TenantsPath}/{tenant.Id}/subscription-type",
                new { subscriptionType = "Yearly" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("subscriptionType").GetString().Should().Be("Yearly");
        }
    }
}
