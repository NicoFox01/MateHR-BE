using System.Net.Http.Headers;
using MateHR.Application.Common.Interfaces;
using MateHR.Domain.Common;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;
using MateHR.Infrastructure.Persistance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MateHR.Api.Tests.Common
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public const string TestIssuer = "test";
        public const string TestAudience = "test";
        public const string TestSigningKey = "clave-de-prueba-con-32-caracteres-minimo";

        private readonly string _databaseName = $"api-tests-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = TestIssuer,
                    ["Jwt:Audience"] = TestAudience,
                    ["Jwt:SigningKey"] = TestSigningKey,
                    ["Jwt:AccessTokenMinutes"] = "60",
                    ["Jwt:RefreshTokenDays"] = "14",
                    ["ConnectionStrings:DefaultConnection"] = "InMemory"
                });
            });

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<DbContextOptions>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options
                        .UseInMemoryDatabase(_databaseName)
                        .UseInternalServiceProvider(inMemoryProvider));
            });
        }

        public HttpClient CreateClientWithoutCookies()
            => CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        public Tenant SeedTenant(
            string name = "Acme Corp",
            string slug = "acme-corp",
            TenantStatus status = TenantStatus.Active)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var tenant = new Tenant(
                name,
                slug,
                "20304050601",
                "owner@acme.com",
                Industry.Technology,
                null,
                status,
                RecruitmentMode.Internal,
                SubscriptionType.Free);

            context.Tenants.Add(tenant);
            context.SaveChanges();
            return tenant;
        }

        public User SeedUser(
            string email,
            string password,
            UserRole role,
            Guid? tenantId = null,
            string firstName = "Test",
            string lastName = "User")
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var user = new User(firstName, lastName, email, hasher.Hash(password), role, tenantId);
            context.Users.Add(user);
            context.SaveChanges();
            return user;
        }

        public HttpClient CreateAuthenticatedClient(User user)
        {
            var jwt = Services.GetRequiredService<IJwtService>();
            var authenticated = new AuthenticatedUser(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.TenantId,
                user.SecurityStamp);

            var token = jwt.GenerateAccessToken(authenticated);
            var client = CreateClientWithoutCookies();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            return client;
        }

        public HttpClient CreateClientAs(UserRole role, Guid? tenantId = null)
        {
            var email = $"{role.ToString().ToLowerInvariant()}@test.com";

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = context.Users.FirstOrDefault(u => u.Email == email);

            user ??= SeedUser(email, "secreta123", role, role == UserRole.SuperAdmin ? null : tenantId ?? Guid.NewGuid());

            return CreateAuthenticatedClient(user);
        }
    }
}
