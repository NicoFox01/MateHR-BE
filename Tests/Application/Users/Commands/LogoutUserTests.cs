using FluentAssertions;
using MateHR.Application.Common.Interfaces;
using MateHR.Application.Users.Commands;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Interfaces;
using Moq;
using Xunit;

namespace MateHR.Application.Tests.Users.Commands
{
    public class LogoutUserTests
    {
        private static RefreshToken ActiveToken(Guid userId)
            => new(userId, "stored-hash", DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(-1));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ExecuteAsync_NullOrWhitespaceToken_DoesNothing(string? token)
        {
            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var refreshTokenService = new Mock<IRefreshTokenService>();

            await new LogoutUser(refreshTokenRepository.Object, refreshTokenService.Object)
                .ExecuteAsync(token!);

            refreshTokenService.VerifyNoOtherCalls();
            refreshTokenRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_UnknownToken_DoesNothing()
        {
            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var refreshTokenService = new Mock<IRefreshTokenService>();

            refreshTokenService.Setup(s => s.ComputeHash("token-x")).Returns("hash-x");
            refreshTokenRepository
                .Setup(r => r.GetByTokenHashAsync("hash-x", It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            await new LogoutUser(refreshTokenRepository.Object, refreshTokenService.Object)
                .ExecuteAsync("token-x");

            refreshTokenRepository.Verify(
                r => r.UpdateRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ValidToken_RevokesAndPersists()
        {
            var token = ActiveToken(Guid.NewGuid());
            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var refreshTokenService = new Mock<IRefreshTokenService>();

            refreshTokenService.Setup(s => s.ComputeHash("token-x")).Returns("hash-x");
            refreshTokenRepository
                .Setup(r => r.GetByTokenHashAsync("hash-x", It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            await new LogoutUser(refreshTokenRepository.Object, refreshTokenService.Object)
                .ExecuteAsync("token-x");

            token.RevokedAt.Should().NotBeNull();
            refreshTokenRepository.Verify(
                r => r.UpdateRefreshTokenAsync(token, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
