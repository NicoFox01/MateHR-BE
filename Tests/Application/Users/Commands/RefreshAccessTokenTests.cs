using FluentAssertions;
using MateHR.Application.Common.Interfaces;
using MateHR.Application.Tests.Common;
using MateHR.Application.Users.Commands;
using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Interfaces;
using MateHR.Domain.Common;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;
using MateHR.Domain.Users.Interfaces;
using Moq;
using Xunit;

namespace MateHR.Application.Tests.Users.Commands
{
    public class RefreshAccessTokenTests
    {
        private static User ActiveUser() => new(
            "Ana",
            "Pérez",
            "ana@acme.com",
            "hash",
            UserRole.Recruiter,
            Guid.NewGuid());

        private static RefreshAccessToken BuildCommand(
            Mock<IRefreshTokenRepository> refreshTokenRepository,
            Mock<IUserRepository> userRepository,
            Mock<IRefreshTokenService> refreshTokenService,
            Mock<IJwtService> jwtService,
            Mock<IUserSecurityStampValidator> stampValidator)
            => new(
                refreshTokenRepository.Object,
                userRepository.Object,
                refreshTokenService.Object,
                jwtService.Object,
                stampValidator.Object,
                TestMapper.Create());

        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task ExecuteAsync_NullOrWhitespaceToken_ThrowsInvalidCredentialsException(string? token)
        {
            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();
            var stampValidator = new Mock<IUserSecurityStampValidator>();

            Func<Task> act = () => BuildCommand(refreshTokenRepository, userRepository, refreshTokenService, jwtService, stampValidator)
                .ExecuteAsync(token);

            var exception = await act.Should().ThrowAsync<InvalidCredentialsException>();
            exception.Which.Message.Should().Be("El refresh token no es valido.");
            refreshTokenService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_UnknownToken_ThrowsAndHashesBeforeLookup()
        {
            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();
            var stampValidator = new Mock<IUserSecurityStampValidator>();

            refreshTokenService.Setup(s => s.ComputeHash("token-inexistente")).Returns("sha-del-token");
            refreshTokenRepository
                .Setup(r => r.GetByTokenHashAsync("sha-del-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            Func<Task> act = () => BuildCommand(refreshTokenRepository, userRepository, refreshTokenService, jwtService, stampValidator)
                .ExecuteAsync("token-inexistente");

            await act.Should().ThrowAsync<InvalidCredentialsException>();
            refreshTokenService.Verify(s => s.ComputeHash("token-inexistente"), Times.Once);
            refreshTokenRepository.Verify(
                r => r.GetByTokenHashAsync("sha-del-token", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ActiveToken_RotatesTokenAndInvalidatesStamp()
        {
            var now = DateTimeOffset.UtcNow;
            var user = ActiveUser();
            var storedToken = new RefreshToken(user.Id, "stored-hash", now.AddDays(1), now.AddDays(-1));

            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();
            var stampValidator = new Mock<IUserSecurityStampValidator>();

            refreshTokenService.Setup(s => s.ComputeHash("stored-token")).Returns("stored-hash");
            refreshTokenRepository
                .Setup(r => r.GetByTokenHashAsync("stored-hash", It.IsAny<CancellationToken>()))
                .ReturnsAsync(storedToken);
            userRepository
                .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            jwtService
                .Setup(j => j.GenerateAccessToken(It.IsAny<AuthenticatedUser>()))
                .Returns(new IssuedToken("new-access", now.AddHours(1)));
            refreshTokenService
                .Setup(s => s.Generate())
                .Returns(new IssuedToken("new-refresh", now.AddDays(14)));
            refreshTokenService
                .Setup(s => s.ComputeHash("new-refresh"))
                .Returns("new-hash");

            var response = await BuildCommand(refreshTokenRepository, userRepository, refreshTokenService, jwtService, stampValidator)
                .ExecuteAsync("stored-token");

            response.AccessToken.Should().Be("new-access");
            response.RefreshToken.Should().Be("new-refresh");
            response.User.Id.Should().Be(user.Id);

            refreshTokenRepository.Verify(
                r => r.UpdateRefreshTokenAsync(It.Is<RefreshToken>(t => t.RevokedAt != null), It.IsAny<CancellationToken>()),
                Times.Once);
            refreshTokenRepository.Verify(
                r => r.CreateRefreshTokenAsync(
                    It.Is<RefreshToken>(t => t.UserId == user.Id && t.TokenHash == "new-hash"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            stampValidator.Verify(v => v.Invalidate(user.Id), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ReusedToken_RevokesAllUserTokensAndThrows()
        {
            var now = DateTimeOffset.UtcNow;
            var user = ActiveUser();
            var storedToken = new RefreshToken(user.Id, "stored-hash", now.AddDays(1), now.AddDays(-1));
            storedToken.Revoke(now.AddMinutes(-5));

            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();
            var stampValidator = new Mock<IUserSecurityStampValidator>();

            refreshTokenService.Setup(s => s.ComputeHash("stored-token")).Returns("stored-hash");
            refreshTokenRepository
                .Setup(r => r.GetByTokenHashAsync("stored-hash", It.IsAny<CancellationToken>()))
                .ReturnsAsync(storedToken);

            Func<Task> act = () => BuildCommand(refreshTokenRepository, userRepository, refreshTokenService, jwtService, stampValidator)
                .ExecuteAsync("stored-token");

            await act.Should().ThrowAsync<InvalidCredentialsException>();
            refreshTokenRepository.Verify(
                r => r.RevokeAllUserRefreshTokensAsync(storedToken.UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ExpiredToken_RevokesAllUserTokensAndThrows()
        {
            var now = DateTimeOffset.UtcNow;
            var user = ActiveUser();
            var storedToken = new RefreshToken(user.Id, "stored-hash", now.AddMinutes(-1), now.AddDays(-2));

            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();
            var stampValidator = new Mock<IUserSecurityStampValidator>();

            refreshTokenService.Setup(s => s.ComputeHash("stored-token")).Returns("stored-hash");
            refreshTokenRepository
                .Setup(r => r.GetByTokenHashAsync("stored-hash", It.IsAny<CancellationToken>()))
                .ReturnsAsync(storedToken);

            Func<Task> act = () => BuildCommand(refreshTokenRepository, userRepository, refreshTokenService, jwtService, stampValidator)
                .ExecuteAsync("stored-token");

            await act.Should().ThrowAsync<InvalidCredentialsException>();
            refreshTokenRepository.Verify(
                r => r.RevokeAllUserRefreshTokensAsync(storedToken.UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_InactiveUser_RevokesAllTokensAndThrows()
        {
            var now = DateTimeOffset.UtcNow;
            var user = ActiveUser();
            user.Deactivate(now);
            var storedToken = new RefreshToken(user.Id, "stored-hash", now.AddDays(1), now.AddDays(-1));

            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();
            var stampValidator = new Mock<IUserSecurityStampValidator>();

            refreshTokenService.Setup(s => s.ComputeHash("stored-token")).Returns("stored-hash");
            refreshTokenRepository
                .Setup(r => r.GetByTokenHashAsync("stored-hash", It.IsAny<CancellationToken>()))
                .ReturnsAsync(storedToken);
            userRepository
                .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            Func<Task> act = () => BuildCommand(refreshTokenRepository, userRepository, refreshTokenService, jwtService, stampValidator)
                .ExecuteAsync("stored-token");

            var exception = await act.Should().ThrowAsync<InvalidCredentialsException>();
            exception.Which.Message.Should().Be("El usuario esta inactivo.");
            refreshTokenRepository.Verify(
                r => r.RevokeAllUserRefreshTokensAsync(user.Id, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
