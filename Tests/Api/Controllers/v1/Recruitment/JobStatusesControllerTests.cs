using FluentAssertions;
using MateHR.Api.Tests.Common;
using MateHR.Domain.Users.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MateHR.Tests.Api.Controllers.v1.Recruitment
{
    public class JobStatusesControllerTests : IDisposable
    {
        private const string JobStatusesPath = "/api/v1/recruitment/job-statuses";

        private readonly CustomWebApplicationFactory _factory = new();

        public void Dispose() => _factory.Dispose();
        [Fact]
        public async Task GetAll_WithoutToken_Returns401()
        {
            var client = _factory.CreateClientWithoutCookies();

            var response = await client.GetAsync(JobStatusesPath);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        }
        [Fact]
        public async Task GetAll_WithEmployeeToken_Returns403()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Employee, tenant.Id);
            var response = await client.GetAsync(JobStatusesPath);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        [Fact]
        public async Task GetAll_WithRecruiterToken_Returns200()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Recruiter, tenant.Id);
            var response = await client.GetAsync(JobStatusesPath);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        [Fact]
        public async Task Create_WithRecruiterToken_Returns403()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Recruiter, tenant.Id);
            var response = await client.PostAsJsonAsync(JobStatusesPath, new { name = "Abierto" });
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        [Fact]
        public async Task Create_WithAdminToken_Returns201WithLocationAndBody()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);
            var response = await client.PostAsJsonAsync(JobStatusesPath, new { name = "Abierto" });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var id = body.GetProperty("id").GetGuid();

            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().Should()
                .Contain($"/api/v1/recruitment/job-statuses/{id}");

            body.GetProperty("name").GetString().Should().Be("Abierto");
            body.GetProperty("tenantId").GetGuid().Should().Be(tenant.Id);

        }
        [Fact]
        public async Task Create_DuplicateName_Returns409()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);
            var response = await client.PostAsJsonAsync(JobStatusesPath, new { name = "Abierto" });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var response2 = await client.PostAsJsonAsync(JobStatusesPath, new { name = "Abierto" });
            response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
        [Fact]
        public async Task Create_EmptyName_Returns400WithErrors()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);
            var response = await client.PostAsJsonAsync(JobStatusesPath, new { name = "" });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            body.TryGetProperty("errors", out var errors).Should().BeTrue();
            errors.ValueKind.Should().Be(JsonValueKind.Object);

        }
        [Fact]
        public async Task GetById_WithRecruiterToken_Returns200()
        {
            var tenant = _factory.SeedTenant();
            var status = _factory.SeedJobStatus(tenant.Id, "Abierto");
            var client = _factory.CreateClientAs(UserRole.Recruiter, tenant.Id);

            var response = await client.GetAsync($"{JobStatusesPath}/{status.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("id").GetGuid().Should().Be(status.Id);
            body.GetProperty("name").GetString().Should().Be("Abierto");
            body.GetProperty("tenantId").GetGuid().Should().Be(tenant.Id);
        }

        [Fact]
        public async Task GetById_UnknownId_Returns404()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.GetAsync($"{JobStatusesPath}/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_ExistingStatus_Returns200WithNewName()
        {
            var tenant = _factory.SeedTenant();
            var status = _factory.SeedJobStatus(tenant.Id, "Activo");
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.PutAsJsonAsync($"{JobStatusesPath}/{status.Id}", new { name = "Pausado" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("id").GetGuid().Should().Be(status.Id);
            body.GetProperty("name").GetString().Should().Be("Pausado");
        }

        [Fact]
        public async Task Delete_ExistingStatus_Returns204()
        {
            var tenant = _factory.SeedTenant();
            var status = _factory.SeedJobStatus(tenant.Id, "Abierto");
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.DeleteAsync($"{JobStatusesPath}/{status.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Delete_UnknownId_Returns404()
        {
            var tenant = _factory.SeedTenant();
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.DeleteAsync($"{JobStatusesPath}/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact(Skip = "pendiente D-01/REC-08: IsInUseAsync siempre devuelve false")]
        public async Task Delete_StatusInUse_Returns409()
        {
            var tenant = _factory.SeedTenant();
            var status = _factory.SeedJobStatus(tenant.Id, "Abierto");
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.DeleteAsync($"{JobStatusesPath}/{status.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task GetAll_WithPaging_Returns200WithTotalAndItems()
        {
            var tenant = _factory.SeedTenant();
            _factory.SeedJobStatus(tenant.Id, "Abierto");
            _factory.SeedJobStatus(tenant.Id, "Cerrado");
            _factory.SeedJobStatus(tenant.Id, "Pausado");
            var client = _factory.CreateClientAs(UserRole.Admin, tenant.Id);

            var response = await client.GetAsync($"{JobStatusesPath}?pageNumber=1&pageSize=10");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("items").GetArrayLength().Should().Be(3);
            body.GetProperty("totalCount").GetInt32().Should().Be(3);
        }
    }
}
