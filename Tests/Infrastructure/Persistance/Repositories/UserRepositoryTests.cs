using FluentAssertions;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;
using MateHR.Infrastructure.Persistance;
using MateHR.Infrastructure.Persistance.Repositories;
using MateHR.Infrastructure.Tests.Common;
using Xunit;

namespace MateHR.Infrastructure.Tests.Persistance.Repositories
{
    public class UserRepositoryTests
    {
        private static User CreateUser(string email = "ana@acme.com") => new(
            "Ana",
            "Pérez",
            email,
            "hash",
            UserRole.Recruiter,
            Guid.NewGuid());

        private static async Task SeedAsync(ApplicationDbContext context, params User[] users)
        {
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingUser_ReturnsUser()
        {
            using var context = InMemoryDbContextFactory.Create();
            var user = CreateUser();
            await SeedAsync(context, user);
            var repository = new UserRepository(context);

            var result = await repository.GetByIdAsync(user.Id);

            result.Id.Should().Be(user.Id);
            result.Email.Should().Be("ana@acme.com");
        }

        [Fact]
        public async Task GetByIdAsync_UnknownId_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new UserRepository(context);
            var unknownId = Guid.NewGuid();

            Func<Task> act = () => repository.GetByIdAsync(unknownId);

            var exception = await act.Should().ThrowAsync<KeyNotFoundException>();
            exception.Which.Message.Should().Contain(unknownId.ToString());
        }

        [Fact]
        public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
        {
            using var context = InMemoryDbContextFactory.Create();
            await SeedAsync(context, CreateUser("ana@acme.com"));
            var repository = new UserRepository(context);

            var result = await repository.GetByEmailAsync("ana@acme.com");

            result.Should().NotBeNull();
            result!.Email.Should().Be("ana@acme.com");
        }

        [Fact]
        public async Task GetByEmailAsync_UnknownEmail_ReturnsNull()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new UserRepository(context);

            var result = await repository.GetByEmailAsync("no@existe.com");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_MixedCaseEmail_FindsUser()
        {
            using var context = InMemoryDbContextFactory.Create();
            await SeedAsync(context, CreateUser("ana@acme.com"));
            var repository = new UserRepository(context);

            var result = await repository.GetByEmailAsync("ANA@ACME.COM ");

            result.Should().NotBeNull();
            result!.Email.Should().Be("ana@acme.com");
        }

        [Fact]
        public async Task ExistsByEmailAsync_ExistingEmail_ReturnsTrue()
        {
            using var context = InMemoryDbContextFactory.Create();
            await SeedAsync(context, CreateUser("ana@acme.com"));
            var repository = new UserRepository(context);

            var result = await repository.ExistsByEmailAsync("ANA@ACME.COM");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByEmailAsync_UnknownEmail_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new UserRepository(context);

            var result = await repository.ExistsByEmailAsync("no@existe.com");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CreateUserAsync_PersistsUser()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new UserRepository(context);
            var user = CreateUser();

            var created = await repository.CreateUserAsync(user);

            created.Id.Should().Be(user.Id);
            var reloaded = await repository.GetByIdAsync(user.Id);
            reloaded.Email.Should().Be("ana@acme.com");
            reloaded.Role.Should().Be(UserRole.Recruiter);
            reloaded.TenantId.Should().Be(user.TenantId);
        }

        [Fact]
        public async Task UpdateUserAsync_SetsUpdatedAt()
        {
            using var context = InMemoryDbContextFactory.Create();
            var user = CreateUser();
            await SeedAsync(context, user);
            var repository = new UserRepository(context);
            var tracked = await repository.GetByIdAsync(user.Id);
            tracked.RecordLogin(tracked.CreatedAt);

            var updated = await repository.UpdateUserAsync(tracked);

            updated.LastLoginAt.Should().Be(user.CreatedAt);
            updated.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}
