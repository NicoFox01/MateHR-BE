using System.Text.RegularExpressions;
using FluentAssertions;
using MateHR.Infrastructure.Authentication;
using MateHR.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Xunit;

namespace MateHR.Infrastructure.Tests.Authentication
{
    public class RefreshTokenServiceTests
    {
        private static RefreshTokenService CreateService(int refreshTokenDays = 14) => new(
            Options.Create(new JwtSettings
            {
                Issuer = "test",
                Audience = "test",
                SigningKey = "clave-de-prueba-con-32-caracteres-minimo",
                AccessTokenMinutes = 60,
                RefreshTokenDays = refreshTokenDays
            }));

        [Fact]
        public void Generate_ReturnsUrlSafeTokenWithoutPadding()
        {
            var service = CreateService();
            var before = DateTimeOffset.UtcNow;

            var token = service.Generate();

            token.Token.Should().NotBeNullOrWhiteSpace();
            token.Token.Should().NotContain("=");
            Regex.IsMatch(token.Token, "^[A-Za-z0-9_-]+$").Should().BeTrue();
            token.ExpiresAt.Should().BeCloseTo(before.AddDays(14), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Generate_Twice_ReturnsDifferentTokens()
        {
            var service = CreateService();

            var first = service.Generate();
            var second = service.Generate();

            first.Token.Should().NotBe(second.Token);
        }

        [Fact]
        public void Generate_ExpiresAtMatchesConfiguredDays()
        {
            var service = CreateService(refreshTokenDays: 14);
            var before = DateTimeOffset.UtcNow;

            var token = service.Generate();

            token.ExpiresAt.Should().BeCloseTo(before.AddDays(14), TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void ComputeHash_SameInput_ReturnsSameHash()
        {
            var service = CreateService();

            var first = service.ComputeHash("token-de-prueba");
            var second = service.ComputeHash("token-de-prueba");

            first.Should().Be(second);
        }

        [Fact]
        public void ComputeHash_Returns64LowercaseHexChars()
        {
            var service = CreateService();

            var hash = service.ComputeHash("token-de-prueba");

            hash.Should().HaveLength(64);
            Regex.IsMatch(hash, "^[0-9a-f]{64}$").Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ComputeHash_NullOrEmpty_ThrowsArgumentException(string? token)
        {
            var service = CreateService();

            Action act = () => service.ComputeHash(token!);

            act.Should().Throw<ArgumentException>();
        }
    }
}
