using FluentAssertions;
using MateHR.Api.Tests.Common;
using MateHR.Domain.Users.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MateHR.Tests.Api.Controllers.v1.Recruitment
{
    public class AreasControllerTests : IDisposable
    {
        private const string AreasPath = "/api/v1/recruitment/areas";

        private readonly CustomWebApplicationFactory _factory = new();

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task GetAll_WithoutToken_Returns401()
        {
            var client = _factory.CreateClientWithoutCookies();

            var response = await client.GetAsync(AreasPath);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAll_WithEmployeeToken_Returns403()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Employee, tenant.Id);

            var response = await client.GetAsync(AreasPath);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetAll_WithRecruiterToken_Returns200()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Recruiter, tenant.Id);

            var response = await client.GetAsync(AreasPath);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Create_WithRecruiterToken_Returns403()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Recruiter, tenant.Id);

            var response = await client.PostAsJsonAsync(AreasPath, new { name = "Ventas" });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Create_WithAdminToken_Returns201WithLocationAndBody()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.PostAsJsonAsync(AreasPath, new { name = "Ventas" });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var id = body.GetProperty("id").GetGuid();

            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().Should()
                .Contain($"/api/v1/recruitment/areas/{id}");

            body.GetProperty("name").GetString().Should().Be("Ventas");
            body.GetProperty("tenantId").GetGuid().Should().Be(tenant.Id);
        }

        [Fact]
        public async Task Create_DuplicateName_Returns409()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.PostAsJsonAsync(AreasPath, new { name = "Ventas" });
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var response2 = await client.PostAsJsonAsync(AreasPath, new { name = "Ventas" });

            response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Create_EmptyName_Returns400WithErrors()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.PostAsJsonAsync(AreasPath, new { name = "" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            body.TryGetProperty("errors", out var errors).Should().BeTrue();
            errors.ValueKind.Should().Be(JsonValueKind.Object);
        }

        [Fact]
        public async Task GetById_WithRecruiterToken_Returns200()
        {
            var tenant = _factory.SeedTenant();
            var area = _factory.SeedArea(tenant.Id, "Ventas");
            var client = _factory.CreateClientAs(UserRole.Recruiter, tenant.Id);

            var response = await client.GetAsync($"{AreasPath}/{area.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("id").GetGuid().Should().Be(area.Id);
            body.GetProperty("name").GetString().Should().Be("Ventas");
            body.GetProperty("tenantId").GetGuid().Should().Be(tenant.Id);
        }

        [Fact]
        public async Task GetById_UnknownId_Returns404()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.GetAsync($"{AreasPath}/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_ExistingArea_Returns200WithNewName()
        {
            var tenant = _factory.SeedTenant();
            var area = _factory.SeedArea(tenant.Id, "Ventas");
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.PutAsJsonAsync($"{AreasPath}/{area.Id}", new { name = "Marketing" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("id").GetGuid().Should().Be(area.Id);
            body.GetProperty("name").GetString().Should().Be("Marketing");
        }

        [Fact]
        public async Task Delete_ExistingArea_Returns204()
        {
            var tenant = _factory.SeedTenant();
            var area = _factory.SeedArea(tenant.Id, "Ventas");
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.DeleteAsync($"{AreasPath}/{area.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Delete_UnknownId_Returns404()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.DeleteAsync($"{AreasPath}/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact(Skip = "pendiente D-01/REC-08: IsInUseAsync siempre devuelve false")]
        public async Task Delete_AreaInUse_Returns409()
        {
            var tenant = _factory.SeedTenant();
            var area = _factory.SeedArea(tenant.Id, "Ventas");
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.DeleteAsync($"{AreasPath}/{area.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task GetAll_WithPaging_Returns200WithTotalAndItems()
        {
            var tenant = _factory.SeedTenant();
            _factory.SeedArea(tenant.Id, "Tecnologia");
            _factory.SeedArea(tenant.Id, "Ventas");
            _factory.SeedArea(tenant.Id, "RRHH");
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.GetAsync($"{AreasPath}?pageNumber=1&pageSize=10");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("items").GetArrayLength().Should().Be(3);
            body.GetProperty("totalCount").GetInt32().Should().Be(3);
        }

        [Fact]
        public async Task Deactivate_WithAdminToken_Returns200WithInactive()
        {
            var tenant = _factory.SeedTenant();
            var area = _factory.SeedArea(tenant.Id, "Ventas");
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.PatchAsync($"{AreasPath}/{area.Id}/deactivate", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("id").GetGuid().Should().Be(area.Id);
            body.GetProperty("isActive").GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task Activate_WithAdminToken_Returns200WithActive()
        {
            var tenant = _factory.SeedTenant();
            var area = _factory.SeedArea(tenant.Id, "Ventas", isActive: false);
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.PatchAsync($"{AreasPath}/{area.Id}/activate", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("id").GetGuid().Should().Be(area.Id);
            body.GetProperty("isActive").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task Deactivate_WithRecruiterToken_Returns403()
        {
            var tenant = _factory.SeedTenant();
            var area = _factory.SeedArea(tenant.Id, "Ventas");
            var client = _factory.CreateClientAs(UserRole.Recruiter, tenant.Id);

            var response = await client.PatchAsync($"{AreasPath}/{area.Id}/deactivate", null);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Deactivate_UnknownId_Returns404()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.PatchAsync($"{AreasPath}/{Guid.NewGuid()}/deactivate", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetAll_IncludeInactiveAsAdmin_IncludesInactive()
        {
            var tenant = _factory.SeedTenant();
            _factory.SeedArea(tenant.Id, "Ventas");
            _factory.SeedArea(tenant.Id, "RRHH", isActive: false);
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.GetAsync($"{AreasPath}?includeInactive=true");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("totalCount").GetInt32().Should().Be(2);
        }

        [Fact]
        public async Task GetAll_IncludeInactiveAsRecruiter_ExcludesInactive()
        {
            var tenant = _factory.SeedTenant();
            _factory.SeedArea(tenant.Id, "Ventas");
            _factory.SeedArea(tenant.Id, "RRHH", isActive: false);
            var client = _factory.CreateClientAs(UserRole.Recruiter, tenant.Id);

            var response = await client.GetAsync($"{AreasPath}?includeInactive=true");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("totalCount").GetInt32().Should().Be(1);
        }

        [Fact]
        public async Task GetById_InactiveAsRecruiter_Returns404()
        {
            var tenant = _factory.SeedTenant();
            var area = _factory.SeedArea(tenant.Id, "RRHH", isActive: false);
            var client = _factory.CreateClientAs(UserRole.Recruiter, tenant.Id);

            var response = await client.GetAsync($"{AreasPath}/{area.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_InactiveAsAdmin_Returns200()
        {
            var tenant = _factory.SeedTenant();
            var area = _factory.SeedArea(tenant.Id, "RRHH", isActive: false);
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.GetAsync($"{AreasPath}/{area.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("isActive").GetBoolean().Should().BeFalse();
        }
    }
}
