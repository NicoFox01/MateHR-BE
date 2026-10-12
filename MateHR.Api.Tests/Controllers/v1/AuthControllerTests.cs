using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MateHR.Api.Tests.Common;
using MateHR.Domain.Users.Enums;
using Xunit;

namespace MateHR.Api.Tests.Controllers.v1
{
    public class AuthControllerTests : IDisposable
    {
        private const string LoginPath = "/api/v1/auth/login";
        private const string RefreshPath = "/api/v1/auth/refresh";
        private const string MePath = "/api/v1/auth/me";
        private const string LogoutPath = "/api/v1/auth/logout";
        private const string CookieName = "matehr_refresh_token";

        private readonly CustomWebApplicationFactory _factory = new();

        public void Dispose() => _factory.Dispose();

        private static object LoginBody(string email, string password) => new { email, password };

        private static string? RefreshCookie(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                return null;
            }

            foreach (var value in values)
            {
                if (value.StartsWith($"{CookieName}=", StringComparison.Ordinal))
                {
                    var cookieValue = value[$"{CookieName}=".Length..];
                    var separator = cookieValue.IndexOf(';');
                    return separator >= 0 ? cookieValue[..separator] : cookieValue;
                }
            }

            return null;
        }

        private static void AttachCookie(HttpRequestMessage request, string cookieValue)
            => request.Headers.Add("Cookie", $"{CookieName}={cookieValue}");

        [Fact]
        public async Task Login_WithValidCredentials_Returns200WithTokensAndCookie()
        {
            var tenantId = Guid.NewGuid();
            _factory.SeedUser("login@acme.com", "secreta123", UserRole.Recruiter, tenantId);
            var client = _factory.CreateClientWithoutCookies();

            var response = await client.PostAsJsonAsync(LoginPath, LoginBody("login@acme.com", "secreta123"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
            body.GetProperty("user").GetProperty("email").GetString().Should().Be("login@acme.com");

            var setCookie = response.Headers.GetValues("Set-Cookie").Single(v => v.StartsWith($"{CookieName}=", StringComparison.Ordinal));
            setCookie.Should().ContainEquivalentOf("HttpOnly");
            setCookie.Should().ContainEquivalentOf("Path=/api/v1/auth");
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_Returns401()
        {
            _factory.SeedUser("badpass@acme.com", "secreta123", UserRole.Recruiter, Guid.NewGuid());
            var client = _factory.CreateClientWithoutCookies();

            var response = await client.PostAsJsonAsync(LoginPath, LoginBody("badpass@acme.com", "clave-mala"));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
            RefreshCookie(response).Should().BeNull();
        }

        [Fact]
        public async Task Login_WithShortPassword_Returns400()
        {
            var client = _factory.CreateClientWithoutCookies();

            var response = await client.PostAsJsonAsync(LoginPath, LoginBody("ana@acme.com", "123"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Object);
        }

        [Fact]
        public async Task Refresh_WithValidCookie_Returns200AndRotatesCookie()
        {
            _factory.SeedUser("refresh@acme.com", "secreta123", UserRole.Recruiter, Guid.NewGuid());
            var client = _factory.CreateClientWithoutCookies();

            var login = await client.PostAsJsonAsync(LoginPath, LoginBody("refresh@acme.com", "secreta123"));
            var originalCookie = RefreshCookie(login);
            originalCookie.Should().NotBeNullOrEmpty();

            var request = new HttpRequestMessage(HttpMethod.Post, RefreshPath);
            AttachCookie(request, originalCookie!);
            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
            var rotatedCookie = RefreshCookie(response);
            rotatedCookie.Should().NotBeNullOrEmpty();
            rotatedCookie.Should().NotBe(originalCookie);
        }

        [Fact]
        public async Task Refresh_WithReusedCookie_Returns401()
        {
            _factory.SeedUser("reuse@acme.com", "secreta123", UserRole.Recruiter, Guid.NewGuid());
            var client = _factory.CreateClientWithoutCookies();

            var login = await client.PostAsJsonAsync(LoginPath, LoginBody("reuse@acme.com", "secreta123"));
            var originalCookie = RefreshCookie(login)!;

            var firstRefresh = new HttpRequestMessage(HttpMethod.Post, RefreshPath);
            AttachCookie(firstRefresh, originalCookie);
            var firstResponse = await client.SendAsync(firstRefresh);
            firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var reused = new HttpRequestMessage(HttpMethod.Post, RefreshPath);
            AttachCookie(reused, originalCookie);
            var response = await client.SendAsync(reused);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Me_WithoutToken_Returns401()
        {
            var client = _factory.CreateClientWithoutCookies();

            var response = await client.GetAsync(MePath);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Me_WithValidToken_Returns200WithUserResponse()
        {
            var user = _factory.SeedUser("me@acme.com", "secreta123", UserRole.Recruiter, Guid.NewGuid());
            var client = _factory.CreateAuthenticatedClient(user);

            var response = await client.GetAsync(MePath);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("id").GetGuid().Should().Be(user.Id);
            body.GetProperty("email").GetString().Should().Be("me@acme.com");
        }

        [Fact]
        public async Task Logout_WithCookie_Returns204AndDeletesCookie()
        {
            _factory.SeedUser("logout@acme.com", "secreta123", UserRole.Recruiter, Guid.NewGuid());
            var client = _factory.CreateClientWithoutCookies();

            var login = await client.PostAsJsonAsync(LoginPath, LoginBody("logout@acme.com", "secreta123"));
            var cookie = RefreshCookie(login)!;

            var request = new HttpRequestMessage(HttpMethod.Post, LogoutPath);
            AttachCookie(request, cookie);
            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var setCookie = response.Headers.GetValues("Set-Cookie").Single(v => v.StartsWith($"{CookieName}=", StringComparison.Ordinal));
            setCookie.Should().Contain($"{CookieName}=");
        }
    }
}
