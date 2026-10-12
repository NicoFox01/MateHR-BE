using FluentAssertions;
using MateHR.Domain.Users.Entities;
using MateHR.Domain.Users.Enums;
using MateHR.Infrastructure.Persistance;
using MateHR.Infrastructure.Services;
using MateHR.Infrastructure.Tests.Common;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace MateHR.Infrastructure.Tests.Services
{
    public class UserSecurityStampValidatorTests
    {
        private static User CreateUser() => new(
            "Ana",
            "Pérez",
            "ana@acme.com",
            "hash",
            UserRole.Recruiter,
            Guid.NewGuid());

        private static async Task SeedAsync(ApplicationDbContext context, User user)
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task IsStampCurrentAsync_EmptyUserId_ReturnsFalseWithoutQuerying()
        {
            using var context = InMemoryDbContextFactory.Create();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var validator = new UserSecurityStampValidator(context, cache);

            var result = await validator.IsStampCurrentAsync(Guid.Empty, Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsStampCurrentAsync_UnknownUser_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var validator = new UserSecurityStampValidator(context, cache);

            var result = await validator.IsStampCurrentAsync(Guid.NewGuid(), Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsStampCurrentAsync_InactiveUser_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var user = CreateUser();
            user.Deactivate(DateTimeOffset.UtcNow);
            await SeedAsync(context, user);
            var validator = new UserSecurityStampValidator(context, cache);

            var result = await validator.IsStampCurrentAsync(user.Id, user.SecurityStamp);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsStampCurrentAsync_MatchingStamp_ReturnsTrue()
        {
            using var context = InMemoryDbContextFactory.Create();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var user = CreateUser();
            await SeedAsync(context, user);
            var validator = new UserSecurityStampValidator(context, cache);

            var result = await validator.IsStampCurrentAsync(user.Id, user.SecurityStamp);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsStampCurrentAsync_DifferentStamp_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var user = CreateUser();
            await SeedAsync(context, user);
            var validator = new UserSecurityStampValidator(context, cache);

            var result = await validator.IsStampCurrentAsync(user.Id, Guid.NewGuid());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsStampCurrentAsync_CacheHitsUntilInvalidate()
        {
            using var context = InMemoryDbContextFactory.Create();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var user = CreateUser();
            await SeedAsync(context, user);
            var validator = new UserSecurityStampValidator(context, cache);
            var oldStamp = user.SecurityStamp;

            (await validator.IsStampCurrentAsync(user.Id, oldStamp)).Should().BeTrue();

            user.ChangePassword("nuevo-hash");
            await context.SaveChangesAsync();
            var newStamp = user.SecurityStamp;

            (await validator.IsStampCurrentAsync(user.Id, oldStamp)).Should().BeTrue();

            validator.Invalidate(user.Id);

            (await validator.IsStampCurrentAsync(user.Id, oldStamp)).Should().BeFalse();
            (await validator.IsStampCurrentAsync(user.Id, newStamp)).Should().BeTrue();
        }
    }
}
