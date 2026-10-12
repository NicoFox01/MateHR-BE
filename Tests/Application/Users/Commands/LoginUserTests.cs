using FluentAssertions;
using FluentValidation;
using MateHR.Application.Common.Interfaces;
using MateHR.Application.Tests.Common;
using MateHR.Application.Users.Commands;
using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Validators;
using MateHR.Domain.Common;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;
using MateHR.Domain.Users.Interfaces;
using Moq;
using Xunit;

namespace MateHR.Application.Tests.Users.Commands
{
    public class LoginUserTests
    {
        private static User ActiveUser(string passwordHash = "hash") => new(
            "Ana",
            "Pérez",
            "ana@acme.com",
            passwordHash,
            UserRole.Recruiter,
            Guid.NewGuid());

        private static LoginUser BuildCommand(
            Mock<IUserRepository> userRepository,
            Mock<IRefreshTokenRepository> refreshTokenRepository,
            Mock<IPasswordHasher> passwordHasher,
            Mock<IRefreshTokenService> refreshTokenService,
            Mock<IJwtService> jwtService)
            => new(
                userRepository.Object,
                refreshTokenRepository.Object,
                passwordHasher.Object,
                refreshTokenService.Object,
                jwtService.Object,
                TestMapper.Create(),
                new LoginRequestValidator());

        private static void SetupSuccessfulTokenIssuance(
            Mock<IRefreshTokenService> refreshTokenService,
            Mock<IJwtService> jwtService,
            Mock<IRefreshTokenRepository> refreshTokenRepository)
        {
            var now = DateTimeOffset.UtcNow;
            jwtService
                .Setup(j => j.GenerateAccessToken(It.IsAny<AuthenticatedUser>()))
                .Returns(new IssuedToken("access-token", now.AddHours(1)));
            refreshTokenService
                .Setup(s => s.Generate())
                .Returns(new IssuedToken("refresh-token", now.AddDays(14)));
            refreshTokenService
                .Setup(s => s.ComputeHash("refresh-token"))
                .Returns("hash-del-refresh");
            refreshTokenRepository
                .Setup(r => r.CreateRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken token, CancellationToken _) => token);
        }

        [Fact]
        public async Task ExecuteAsync_ValidCredentials_ReturnsTokensAndMappedUser()
        {
            var user = ActiveUser("hash");
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();

            userRepository
                .Setup(r => r.GetByEmailAsync("ana@acme.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            passwordHasher.Setup(h => h.Verify("secreta123", "hash")).Returns(true);
            SetupSuccessfulTokenIssuance(refreshTokenService, jwtService, refreshTokenRepository);

            var response = await BuildCommand(userRepository, refreshTokenRepository, passwordHasher, refreshTokenService, jwtService)
                .ExecuteAsync(new LoginRequest { Email = "ana@acme.com", Password = "secreta123" });

            response.AccessToken.Should().Be("access-token");
            response.RefreshToken.Should().Be("refresh-token");
            response.User.Id.Should().Be(user.Id);
            response.User.Email.Should().Be("ana@acme.com");

            refreshTokenRepository.Verify(
                r => r.InvalidateExpiredUserRefreshTokensAsync(user.Id, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Once);
            refreshTokenRepository.Verify(
                r => r.CreateRefreshTokenAsync(
                    It.Is<RefreshToken>(t => t.TokenHash == "hash-del-refresh" && t.UserId == user.Id),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            userRepository.Verify(r => r.UpdateUserAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_UnknownEmail_ThrowsInvalidCredentialsException()
        {
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();

            userRepository
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            Func<Task> act = () => BuildCommand(userRepository, refreshTokenRepository, passwordHasher, refreshTokenService, jwtService)
                .ExecuteAsync(new LoginRequest { Email = "nadie@acme.com", Password = "secreta123" });

            var exception = await act.Should().ThrowAsync<InvalidCredentialsException>();
            exception.Which.Message.Should().Be("Las credenciales son invalidas.");
            jwtService.Verify(j => j.GenerateAccessToken(It.IsAny<AuthenticatedUser>()), Times.Never);
            refreshTokenRepository.Verify(
                r => r.CreateRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WrongPassword_ThrowsSameInvalidCredentialsException()
        {
            var user = ActiveUser("hash");
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();

            userRepository
                .Setup(r => r.GetByEmailAsync("ana@acme.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            Func<Task> act = () => BuildCommand(userRepository, refreshTokenRepository, passwordHasher, refreshTokenService, jwtService)
                .ExecuteAsync(new LoginRequest { Email = "ana@acme.com", Password = "otra-clave" });

            var exception = await act.Should().ThrowAsync<InvalidCredentialsException>();
            exception.Which.Message.Should().Be("Las credenciales son invalidas.");
        }

        [Fact]
        public async Task ExecuteAsync_InactiveUser_ThrowsInvalidCredentialsExceptionWithInactiveMessage()
        {
            var user = ActiveUser("hash");
            user.Deactivate(DateTimeOffset.UtcNow);
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();

            userRepository
                .Setup(r => r.GetByEmailAsync("ana@acme.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            Func<Task> act = () => BuildCommand(userRepository, refreshTokenRepository, passwordHasher, refreshTokenService, jwtService)
                .ExecuteAsync(new LoginRequest { Email = "ana@acme.com", Password = "secreta123" });

            var exception = await act.Should().ThrowAsync<InvalidCredentialsException>();
            exception.Which.Message.Should().Be("El usuario esta inactivo.");
            refreshTokenRepository.Verify(
                r => r.CreateRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                Times.Never);
            refreshTokenRepository.Verify(
                r => r.RevokeAllUserRefreshTokensAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShortPassword_ThrowsValidationException()
        {
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();

            Func<Task> act = () => BuildCommand(userRepository, refreshTokenRepository, passwordHasher, refreshTokenService, jwtService)
                .ExecuteAsync(new LoginRequest { Email = "ana@acme.com", Password = "123" });

            await act.Should().ThrowAsync<ValidationException>();
            userRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_MixedCaseEmail_NormalizesBeforeQuery()
        {
            var user = ActiveUser("hash");
            var userRepository = new Mock<IUserRepository>();
            var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var refreshTokenService = new Mock<IRefreshTokenService>();
            var jwtService = new Mock<IJwtService>();

            userRepository
                .Setup(r => r.GetByEmailAsync("ana@acme.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            SetupSuccessfulTokenIssuance(refreshTokenService, jwtService, refreshTokenRepository);

            await BuildCommand(userRepository, refreshTokenRepository, passwordHasher, refreshTokenService, jwtService)
                .ExecuteAsync(new LoginRequest { Email = "  ANA@ACME.COM  ", Password = "secreta123" });

            userRepository.Verify(
                r => r.GetByEmailAsync("ana@acme.com", It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
