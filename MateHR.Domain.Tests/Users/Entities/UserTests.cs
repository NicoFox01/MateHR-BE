using FluentAssertions;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;
using Xunit;

namespace MateHR.Domain.Tests.Users.Entities
{
    public class UserTests
    {
        private static User CreateUser(
            string firstName = "Ana",
            string lastName = "Pérez",
            string email = "ana@acme.com",
            string passwordHash = "hash-123",
            UserRole role = UserRole.Recruiter,
            Guid? tenantId = null)
            => new(firstName, lastName, email, passwordHash, role, tenantId ?? Guid.NewGuid());

        [Fact]
        public void Constructor_ValidUserWithTenant_InitializesUser()
        {
            var tenantId = Guid.NewGuid();

            var user = new User("Ana", "Pérez", "ana@acme.com", "hash-123", UserRole.Recruiter, tenantId);

            user.Id.Should().NotBe(Guid.Empty);
            user.Role.Should().Be(UserRole.Recruiter);
            user.TenantId.Should().Be(tenantId);
            user.IsActive.Should().BeTrue();
            user.SecurityStamp.Should().NotBe(Guid.Empty);
            user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Constructor_SuperAdminWithTenant_ThrowsArgumentException()
        {
            Action act = () => new User("Ana", "Pérez", "ana@acme.com", "hash-123", UserRole.SuperAdmin, Guid.NewGuid());

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("tenantId");
        }

        [Fact]
        public void Constructor_RoleWithoutTenant_ThrowsArgumentException()
        {
            Action act = () => new User("Ana", "Pérez", "ana@acme.com", "hash-123", UserRole.Admin, null);

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("tenantId");
        }

        [Fact]
        public void Constructor_TenantIdEmpty_ThrowsArgumentException()
        {
            Action act = () => new User("Ana", "Pérez", "ana@acme.com", "hash-123", UserRole.Employee, Guid.Empty);

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("tenantId");
        }

        [Fact]
        public void Constructor_UndefinedRole_ThrowsArgumentOutOfRangeException()
        {
            Action act = () => new User("Ana", "Pérez", "ana@acme.com", "hash-123", (UserRole)999, Guid.NewGuid());

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_MixedCaseEmail_NormalizesToLowercase()
        {
            var user = new User("Ana", "Pérez", "  ANA.PEREZ@ACME.COM  ", "hash-123", UserRole.Recruiter, Guid.NewGuid());

            user.Email.Should().Be("ana.perez@acme.com");
        }

        [Fact]
        public void Constructor_WhitespaceFirstName_ThrowsArgumentException()
        {
            Action act = () => CreateUser(firstName: "  ");

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("firstName");
        }

        [Fact]
        public void Constructor_WhitespaceLastName_ThrowsArgumentException()
        {
            Action act = () => CreateUser(lastName: "  ");

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("lastName");
        }

        [Fact]
        public void Constructor_WhitespacePasswordHash_ThrowsArgumentException()
        {
            Action act = () => CreateUser(passwordHash: "");

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("passwordHash");
        }

        [Fact]
        public void RecordLogin_ValidMoment_UpdatesLastLoginAt()
        {
            var user = CreateUser();
            var moment = DateTimeOffset.UtcNow.AddMinutes(1);

            user.RecordLogin(moment);

            user.LastLoginAt.Should().Be(moment);
            user.UpdatedAt.Should().Be(moment);
        }

        [Fact]
        public void RecordLogin_MomentBeforeCreatedAt_ThrowsArgumentOutOfRangeException()
        {
            var user = CreateUser();
            var moment = user.CreatedAt.AddSeconds(-1);

            Action act = () => user.RecordLogin(moment);

            act.Should().Throw<ArgumentOutOfRangeException>();
            user.LastLoginAt.Should().BeNull();
        }

        [Fact]
        public void ChangePassword_NewHash_UpdatesHashAndRegeneratesSecurityStamp()
        {
            var user = CreateUser();
            var originalStamp = user.SecurityStamp;

            user.ChangePassword("hash-nuevo");

            user.PasswordHash.Should().Be("hash-nuevo");
            user.SecurityStamp.Should().NotBe(originalStamp);
            user.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void ChangePassword_WhitespaceHash_ThrowsArgumentException()
        {
            var user = CreateUser();
            var originalHash = user.PasswordHash;
            var originalStamp = user.SecurityStamp;

            Action act = () => user.ChangePassword("   ");

            act.Should().Throw<ArgumentException>();
            user.PasswordHash.Should().Be(originalHash);
            user.SecurityStamp.Should().Be(originalStamp);
        }

        [Fact]
        public void Deactivate_ActiveUser_DeactivatesAndRegeneratesStamp()
        {
            var user = CreateUser();
            var originalStamp = user.SecurityStamp;
            var moment = DateTimeOffset.UtcNow;

            user.Deactivate(moment);

            user.IsActive.Should().BeFalse();
            user.SecurityStamp.Should().NotBe(originalStamp);
            user.UpdatedAt.Should().Be(moment);
        }

        [Fact]
        public void Deactivate_AlreadyInactive_ThrowsInvalidOperationException()
        {
            var user = CreateUser();
            user.Deactivate(DateTimeOffset.UtcNow);

            Action act = () => user.Deactivate(DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Activate_InactiveUser_ActivatesAndRegeneratesStamp()
        {
            var user = CreateUser();
            user.Deactivate(DateTimeOffset.UtcNow);
            var stampAfterDeactivate = user.SecurityStamp;
            var moment = DateTimeOffset.UtcNow;

            user.Activate(moment);

            user.IsActive.Should().BeTrue();
            user.SecurityStamp.Should().NotBe(stampAfterDeactivate);
            user.UpdatedAt.Should().Be(moment);
        }

        [Fact]
        public void Activate_AlreadyActive_ThrowsInvalidOperationException()
        {
            var user = CreateUser();

            Action act = () => user.Activate(DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void CanAuthenticate_ActiveUser_ReturnsTrue()
        {
            var user = CreateUser();

            user.CanAuthenticate().Should().BeTrue();
        }

        [Fact]
        public void CanAuthenticate_InactiveUser_ReturnsFalse()
        {
            var user = CreateUser();
            user.Deactivate(DateTimeOffset.UtcNow);

            user.CanAuthenticate().Should().BeFalse();
        }
    }
}
