using FluentAssertions;
using MateHR.Domain.Users.Entities;
using MateHR.Infrastructure.Persistance;
using MateHR.Infrastructure.Persistance.Repositories;
using MateHR.Infrastructure.Tests.Common;
using Xunit;

namespace MateHR.Infrastructure.Tests.Persistance.Repositories
{
    public class RefreshTokenRepositoryTests
    {
        private static RefreshToken CreateToken(
            Guid userId,
            string tokenHash,
            DateTimeOffset createdAt,
            DateTimeOffset expiresAt)
            => new(userId, tokenHash, expiresAt, createdAt);

        private static async Task SeedAsync(ApplicationDbContext context, params RefreshToken[] tokens)
        {
            context.RefreshTokens.AddRange(tokens);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetByTokenHashAsync_ExistingHash_ReturnsToken()
        {
            using var context = InMemoryDbContextFactory.Create();
            var token = CreateToken(Guid.NewGuid(), "sha-1", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            await SeedAsync(context, token);
            var repository = new RefreshTokenRepository(context);

            var result = await repository.GetByTokenHashAsync("sha-1");

            result.Should().NotBeNull();
            result!.TokenHash.Should().Be("sha-1");
        }

        [Fact]
        public async Task GetByTokenHashAsync_UnknownHash_ReturnsNull()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new RefreshTokenRepository(context);

            var result = await repository.GetByTokenHashAsync("sha-inexistente");

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateRefreshTokenAsync_PersistsToken()
        {
            using var context = InMemoryDbContextFactory.Create();
            var userId = Guid.NewGuid();
            var token = CreateToken(userId, "sha-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(14));
            var repository = new RefreshTokenRepository(context);

            var created = await repository.CreateRefreshTokenAsync(token);

            created.Id.Should().Be(token.Id);
            var reloaded = await repository.GetByTokenHashAsync("sha-1");
            reloaded.Should().NotBeNull();
            reloaded!.UserId.Should().Be(userId);
            reloaded.ExpiresAt.Should().Be(token.ExpiresAt);
        }

        [Fact]
        public async Task UpdateRefreshTokenAsync_PersistsRevocation()
        {
            using var context = InMemoryDbContextFactory.Create();
            var token = CreateToken(Guid.NewGuid(), "sha-1", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            await SeedAsync(context, token);
            var repository = new RefreshTokenRepository(context);
            var tracked = await repository.GetByTokenHashAsync("sha-1");
            var now = DateTimeOffset.UtcNow;
            tracked!.Revoke(now);

            await repository.UpdateRefreshTokenAsync(tracked);

            var reloaded = await repository.GetByTokenHashAsync("sha-1");
            reloaded!.RevokedAt.Should().Be(now);
        }

        [Fact]
        public async Task InvalidateExpiredUserRefreshTokensAsync_RevokesOnlyExpiredAndReturnsCount()
        {
            using var context = InMemoryDbContextFactory.Create();
            var userId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var expired = CreateToken(userId, "expired", now.AddDays(-10), now.AddDays(-1));
            var valid = CreateToken(userId, "valid", now.AddDays(-1), now.AddDays(5));
            var alreadyRevoked = CreateToken(userId, "revoked", now.AddDays(-10), now.AddDays(-1));
            var originalRevocation = now.AddDays(-2);
            alreadyRevoked.Revoke(originalRevocation);

            await SeedAsync(context, expired, valid, alreadyRevoked);
            var repository = new RefreshTokenRepository(context);

            var count = await repository.InvalidateExpiredUserRefreshTokensAsync(userId, now);

            count.Should().Be(1);
            expired.RevokedAt.Should().Be(now);
            valid.RevokedAt.Should().BeNull();
            alreadyRevoked.RevokedAt.Should().Be(originalRevocation);
        }

        [Fact]
        public async Task RevokeAllUserRefreshTokensAsync_RevokesOnlyUsersActiveTokens()
        {
            using var context = InMemoryDbContextFactory.Create();
            var now = DateTimeOffset.UtcNow;
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();

            var tokenA1 = CreateToken(userA, "a-1", now.AddDays(-1), now.AddDays(5));
            var tokenA2 = CreateToken(userA, "a-2", now.AddDays(-1), now.AddDays(5));
            var tokenB = CreateToken(userB, "b-1", now.AddDays(-1), now.AddDays(5));

            await SeedAsync(context, tokenA1, tokenA2, tokenB);
            var repository = new RefreshTokenRepository(context);

            var count = await repository.RevokeAllUserRefreshTokensAsync(userA, now);

            count.Should().Be(2);
            tokenA1.RevokedAt.Should().Be(now);
            tokenA2.RevokedAt.Should().Be(now);
            tokenB.RevokedAt.Should().BeNull();
        }
    }
}
