using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using MateHR.Domain.Common;
using MateHR.Domain.Users.Enums;
using MateHR.Infrastructure.Authentication;
using MateHR.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace MateHR.Infrastructure.Tests.Authentication
{
    public class JwtServiceTests
    {
        private const string Issuer = "test";
        private const string Audience = "test";
        private const string SigningKey = "clave-de-prueba-con-32-caracteres-minimo";

        private static JwtSettings ValidSettings() => new()
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = SigningKey,
            AccessTokenMinutes = 60,
            RefreshTokenDays = 14
        };

        private static JwtService CreateService(JwtSettings settings) => new(Options.Create(settings));

        private static AuthenticatedUser CreateUser(Guid? tenantId) => new(
            Guid.NewGuid(),
            "Ana",
            "Pérez",
            "ana@acme.com",
            UserRole.SuperAdmin,
            tenantId,
            Guid.NewGuid());

        private static ClaimsPrincipal ValidateToken(string token)
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            return handler.ValidateToken(token, parameters, out _);
        }

        [Fact]
        public void Constructor_MissingIssuer_ThrowsInvalidOperationException()
        {
            var settings = ValidSettings();
            settings.Issuer = "";

            Action act = () => CreateService(settings);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Constructor_MissingAudience_ThrowsInvalidOperationException()
        {
            var settings = ValidSettings();
            settings.Audience = "";

            Action act = () => CreateService(settings);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Constructor_MissingSigningKey_ThrowsInvalidOperationException()
        {
            var settings = ValidSettings();
            settings.SigningKey = "";

            Action act = () => CreateService(settings);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Constructor_ShortSigningKey_ThrowsInvalidOperationException()
        {
            var settings = ValidSettings();
            settings.SigningKey = "corta-key1";

            Action act = () => CreateService(settings);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Constructor_AccessTokenMinutesZero_ThrowsInvalidOperationException()
        {
            var settings = ValidSettings();
            settings.AccessTokenMinutes = 0;

            Action act = () => CreateService(settings);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Constructor_RefreshTokenDaysZero_ThrowsInvalidOperationException()
        {
            var settings = ValidSettings();
            settings.RefreshTokenDays = 0;

            Action act = () => CreateService(settings);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GenerateAccessToken_NullUser_ThrowsArgumentNullException()
        {
            var service = CreateService(ValidSettings());

            Action act = () => service.GenerateAccessToken(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GenerateAccessToken_ValidUser_ReturnsValidTokenWithClaims()
        {
            var service = CreateService(ValidSettings());
            var user = CreateUser(tenantId: null);

            var issued = service.GenerateAccessToken(user);
            var principal = ValidateToken(issued.Token);

            principal.FindFirst(AuthClaims.UserId)!.Value.Should().Be(user.Id.ToString());
            principal.FindFirst(AuthClaims.Email)!.Value.Should().Be("ana@acme.com");
            principal.FindFirst(AuthClaims.Role)!.Value.Should().Be("SuperAdmin");
            principal.FindFirst(AuthClaims.SecurityStamp)!.Value.Should().Be(user.SecurityStamp.ToString());
        }

        [Fact]
        public void GenerateAccessToken_UserWithTenant_IncludesTenantClaim()
        {
            var service = CreateService(ValidSettings());
            var tenantId = Guid.NewGuid();
            var user = CreateUser(tenantId);

            var issued = service.GenerateAccessToken(user);
            var principal = ValidateToken(issued.Token);

            principal.FindFirst(AuthClaims.TenantId)!.Value.Should().Be(tenantId.ToString());
        }

        [Fact]
        public void GenerateAccessToken_UserWithoutTenant_OmitsTenantClaim()
        {
            var service = CreateService(ValidSettings());
            var user = CreateUser(tenantId: null);

            var issued = service.GenerateAccessToken(user);
            var principal = ValidateToken(issued.Token);

            principal.FindFirst(AuthClaims.TenantId).Should().BeNull();
        }

        [Fact]
        public void GenerateAccessToken_ExpiresAtMatchesConfiguredMinutes()
        {
            var service = CreateService(ValidSettings());
            var user = CreateUser(tenantId: null);
            var before = DateTimeOffset.UtcNow;

            var issued = service.GenerateAccessToken(user);

            issued.ExpiresAt.Should().BeCloseTo(before.AddMinutes(60), TimeSpan.FromSeconds(5));
        }
    }
}
