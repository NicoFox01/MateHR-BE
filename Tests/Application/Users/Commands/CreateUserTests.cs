using FluentAssertions;
using FluentValidation;
using MateHR.Application.Common.Interfaces;
using MateHR.Application.Tests.Common;
using MateHR.Application.Users.Commands;
using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Interfaces;
using MateHR.Application.Users.Validators;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.Interfaces;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;
using MateHR.Domain.Users.Interfaces;
using Moq;
using Xunit;

namespace MateHR.Application.Tests.Users.Commands
{
    public class CreateUserTests
    {
        private static Tenant ExistingTenant() => new(
            "Acme Corp",
            "acme-corp",
            "20304050601",
            "owner@acme.com",
            Industry.Technology,
            null,
            TenantStatus.Active,
            RecruitmentMode.Internal,
            SubscriptionType.Free);

        private static CreateUser BuildCommand(
            Mock<IUserRepository> userRepository,
            Mock<ITenantRepository> tenantRepository,
            Mock<IPasswordHasher> passwordHasher,
            Mock<IUserSecurityStampValidator> stampValidator)
            => new(
                userRepository.Object,
                tenantRepository.Object,
                passwordHasher.Object,
                stampValidator.Object,
                TestMapper.Create(),
                new CreateUserRequestValidator());

        [Fact]
        public async Task ExecuteAsync_ValidRequest_HashesPasswordAndReturnsMappedUser()
        {
            var tenant = ExistingTenant();
            var userRepository = new Mock<IUserRepository>();
            var tenantRepository = new Mock<ITenantRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var stampValidator = new Mock<IUserSecurityStampValidator>();

            User? captured = null;
            userRepository
                .Setup(r => r.ExistsByEmailAsync("nueva@acme.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            tenantRepository
                .Setup(r => r.GetTenantByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
            passwordHasher.Setup(h => h.Hash("secreta123")).Returns("hash-abc");
            userRepository
                .Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User u, CancellationToken _) =>
                {
                    captured = u;
                    return u;
                });

            var response = await BuildCommand(userRepository, tenantRepository, passwordHasher, stampValidator)
                .ExecuteAsync(new CreateUserRequest
                {
                    FirstName = "Ana",
                    LastName = "Pérez",
                    Email = "  NUEVA@ACME.COM ",
                    Password = "secreta123",
                    Role = UserRole.Recruiter,
                    TenantId = tenant.Id
                });

            captured.Should().NotBeNull();
            captured!.Email.Should().Be("nueva@acme.com");
            captured.PasswordHash.Should().Be("hash-abc");
            captured.TenantId.Should().Be(tenant.Id);
            response.Id.Should().Be(captured.Id);
            response.Email.Should().Be("nueva@acme.com");
            stampValidator.Verify(v => v.Invalidate(captured.Id), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_DuplicateEmail_ThrowsEmailAlreadyExistsException()
        {
            var userRepository = new Mock<IUserRepository>();
            var tenantRepository = new Mock<ITenantRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var stampValidator = new Mock<IUserSecurityStampValidator>();

            userRepository
                .Setup(r => r.ExistsByEmailAsync("nueva@acme.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            Func<Task> act = () => BuildCommand(userRepository, tenantRepository, passwordHasher, stampValidator)
                .ExecuteAsync(new CreateUserRequest
                {
                    FirstName = "Ana",
                    LastName = "Pérez",
                    Email = "nueva@acme.com",
                    Password = "secreta123",
                    Role = UserRole.Recruiter,
                    TenantId = Guid.NewGuid()
                });

            var exception = await act.Should().ThrowAsync<EmailAlreadyExistsException>();
            exception.Which.Email.Should().Be("nueva@acme.com");
            userRepository.Verify(
                r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_TenantNotFound_PropagatesException()
        {
            var tenantId = Guid.NewGuid();
            var userRepository = new Mock<IUserRepository>();
            var tenantRepository = new Mock<ITenantRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var stampValidator = new Mock<IUserSecurityStampValidator>();

            userRepository
                .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            tenantRepository
                .Setup(r => r.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException());

            Func<Task> act = () => BuildCommand(userRepository, tenantRepository, passwordHasher, stampValidator)
                .ExecuteAsync(new CreateUserRequest
                {
                    FirstName = "Ana",
                    LastName = "Pérez",
                    Email = "nueva@acme.com",
                    Password = "secreta123",
                    Role = UserRole.Recruiter,
                    TenantId = tenantId
                });

            await act.Should().ThrowAsync<KeyNotFoundException>();
            userRepository.Verify(
                r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_SuperAdminWithTenant_ThrowsValidationException()
        {
            var userRepository = new Mock<IUserRepository>();
            var tenantRepository = new Mock<ITenantRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var stampValidator = new Mock<IUserSecurityStampValidator>();

            Func<Task> act = () => BuildCommand(userRepository, tenantRepository, passwordHasher, stampValidator)
                .ExecuteAsync(new CreateUserRequest
                {
                    FirstName = "Ana",
                    LastName = "Pérez",
                    Email = "nueva@acme.com",
                    Password = "secreta123",
                    Role = UserRole.SuperAdmin,
                    TenantId = Guid.NewGuid()
                });

            await act.Should().ThrowAsync<ValidationException>();
            userRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_ShortPassword_ThrowsValidationException()
        {
            var userRepository = new Mock<IUserRepository>();
            var tenantRepository = new Mock<ITenantRepository>();
            var passwordHasher = new Mock<IPasswordHasher>();
            var stampValidator = new Mock<IUserSecurityStampValidator>();

            Func<Task> act = () => BuildCommand(userRepository, tenantRepository, passwordHasher, stampValidator)
                .ExecuteAsync(new CreateUserRequest
                {
                    FirstName = "Ana",
                    LastName = "Pérez",
                    Email = "nueva@acme.com",
                    Password = "123",
                    Role = UserRole.Recruiter,
                    TenantId = Guid.NewGuid()
                });

            await act.Should().ThrowAsync<ValidationException>();
            userRepository.VerifyNoOtherCalls();
        }
    }
}
