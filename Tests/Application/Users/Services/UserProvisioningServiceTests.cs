using FluentAssertions;
using MateHR.Application.Common.Interfaces;
using MateHR.Application.Users.Services;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;
using MateHR.Domain.Users.Interfaces;
using Moq;
using Xunit;

namespace MateHR.Application.Tests.Users.Services
{
    public class UserProvisioningServiceTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("1234567")]
        public async Task CreateAsync_InvalidPassword_ThrowsArgumentException(string password)
        {
            var userRepository = new Mock<IUserRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();

            Func<Task> act = () => new UserProvisioningService(userRepository.Object, passwordHasher.Object)
                .CreateAsync("Ana", "Pérez", "ana@acme.com", password, UserRole.Recruiter, Guid.NewGuid());

            var exception = await act.Should().ThrowAsync<ArgumentException>();
            exception.Which.ParamName.Should().Be("plainTextPassword");
            userRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateAsync_DuplicateEmail_ThrowsEmailAlreadyExistsException()
        {
            var userRepository = new Mock<IUserRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();

            userRepository
                .Setup(r => r.ExistsByEmailAsync("ana@acme.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            Func<Task> act = () => new UserProvisioningService(userRepository.Object, passwordHasher.Object)
                .CreateAsync("Ana", "Pérez", "  ANA@ACME.COM  ", "secreta123", UserRole.Recruiter, Guid.NewGuid());

            var exception = await act.Should().ThrowAsync<EmailAlreadyExistsException>();
            exception.Which.Email.Should().Be("ana@acme.com");
            passwordHasher.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_NormalizesEmailAndHashesPassword()
        {
            var userRepository = new Mock<IUserRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var tenantId = Guid.NewGuid();

            userRepository
                .Setup(r => r.ExistsByEmailAsync("ana@acme.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            passwordHasher.Setup(h => h.Hash("secreta123")).Returns("hash-1");
            userRepository
                .Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User u, CancellationToken _) => u);

            var user = await new UserProvisioningService(userRepository.Object, passwordHasher.Object)
                .CreateAsync(" Ana ", " Pérez ", "  ANA@ACME.COM ", "secreta123", UserRole.Recruiter, tenantId);

            user.Email.Should().Be("ana@acme.com");
            user.PasswordHash.Should().Be("hash-1");
            user.TenantId.Should().Be(tenantId);
        }

        [Fact]
        public async Task CreateAsync_SuperAdminWithoutTenant_Succeeds()
        {
            var userRepository = new Mock<IUserRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();

            userRepository
                .Setup(r => r.ExistsByEmailAsync("root@acme.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            passwordHasher.Setup(h => h.Hash("secreta123")).Returns("hash-1");
            userRepository
                .Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User u, CancellationToken _) => u);

            var user = await new UserProvisioningService(userRepository.Object, passwordHasher.Object)
                .CreateAsync("Root", "Admin", "root@acme.com", "secreta123", UserRole.SuperAdmin, null);

            user.Role.Should().Be(UserRole.SuperAdmin);
            user.TenantId.Should().BeNull();
        }
    }
}
