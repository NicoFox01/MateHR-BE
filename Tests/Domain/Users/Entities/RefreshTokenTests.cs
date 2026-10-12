using FluentAssertions;
using MateHR.Domain.Users.Entities;
using Xunit;

namespace MateHR.Domain.Tests.Users.Entities
{
    public class RefreshTokenTests
    {
        private static RefreshToken CreateToken(
            Guid? userId = null,
            string tokenHash = "abc123",
            DateTimeOffset? createdAt = null,
            DateTimeOffset? expiresAt = null)
        {
            var created = createdAt ?? DateTimeOffset.UtcNow;
            return new RefreshToken(
                userId ?? Guid.NewGuid(),
                tokenHash,
                expiresAt ?? created.AddDays(14),
                created);
        }

        [Fact]
        public void Constructor_ValidData_CreatesToken()
        {
            var userId = Guid.NewGuid();
            var createdAt = DateTimeOffset.UtcNow;
            var expiresAt = createdAt.AddDays(14);

            var token = new RefreshToken(userId, "abc123", expiresAt, createdAt);

            token.Id.Should().NotBe(Guid.Empty);
            token.UserId.Should().Be(userId);
            token.TokenHash.Should().Be("abc123");
            token.RevokedAt.Should().BeNull();
            token.ReplacedAt.Should().BeNull();
            token.ExpiresAt.Should().Be(expiresAt);
            token.CreatedAt.Should().Be(createdAt);
        }

        [Fact]
        public void Constructor_EmptyUserId_ThrowsArgumentException()
        {
            Action act = () => CreateToken(userId: Guid.Empty);

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("userId");
        }

        [Fact]
        public void Constructor_ExpiresAtBeforeCreatedAt_ThrowsArgumentOutOfRangeException()
        {
            var now = DateTimeOffset.UtcNow;

            Action act = () => CreateToken(createdAt: now, expiresAt: now.AddMinutes(-1));

            act.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("expiresAt");
        }

        [Fact]
        public void Constructor_ExpiresAtEqualsCreatedAt_ThrowsArgumentOutOfRangeException()
        {
            var now = DateTimeOffset.UtcNow;

            Action act = () => CreateToken(createdAt: now, expiresAt: now);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_WhitespaceTokenHash_ThrowsArgumentException()
        {
            Action act = () => CreateToken(tokenHash: "");

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("tokenHash");
        }

        [Fact]
        public void Constructor_TokenHashTooLong_ThrowsArgumentException()
        {
            Action act = () => CreateToken(tokenHash: new string('a', 65));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Revoke_ValidMoment_SetsRevokedAt()
        {
            var token = CreateToken();
            var moment = DateTimeOffset.UtcNow;

            token.Revoke(moment);

            token.RevokedAt.Should().Be(moment);
        }

        [Fact]
        public void Revoke_Twice_KeepsOriginalRevokedAt()
        {
            var token = CreateToken();
            var moment1 = DateTimeOffset.UtcNow;
            var moment2 = moment1.AddMinutes(5);

            token.Revoke(moment1);
            token.Revoke(moment2);

            token.RevokedAt.Should().Be(moment1);
        }

        [Fact]
        public void ReplaceWith_SetsRevokedAtAndReplacedAt()
        {
            var token = CreateToken();
            var moment = DateTimeOffset.UtcNow;

            token.ReplaceWith(moment);

            token.RevokedAt.Should().Be(moment);
            token.ReplacedAt.Should().Be(moment);
        }

        [Fact]
        public void IsActive_ActiveToken_ReturnsTrue()
        {
            var now = DateTimeOffset.UtcNow;
            var token = CreateToken(createdAt: now, expiresAt: now.AddDays(1));

            token.IsActive(now).Should().BeTrue();
        }

        [Fact]
        public void IsActive_RevokedToken_ReturnsFalse()
        {
            var now = DateTimeOffset.UtcNow;
            var token = CreateToken(createdAt: now, expiresAt: now.AddDays(1));
            token.Revoke(now);

            token.IsActive(now.AddMinutes(1)).Should().BeFalse();
        }

        [Fact]
        public void IsActive_ExpiredToken_ReturnsFalse()
        {
            var now = DateTimeOffset.UtcNow;
            var token = CreateToken(createdAt: now.AddDays(-1), expiresAt: now);

            token.IsActive(now.AddSeconds(1)).Should().BeFalse();
        }
    }
}
