using FluentAssertions;
using MateHR.Infrastructure.Authentication;
using Xunit;

namespace MateHR.Infrastructure.Tests.Authentication
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _hasher = new();

        [Fact]
        public void Hash_ValidPassword_ReturnsFourPartPbkdf2Format()
        {
            var hash = _hasher.Hash("secreta123");

            var parts = hash.Split('$');
            parts.Should().HaveCount(4);
            parts[0].Should().Be("pbkdf2-sha256");
            int.TryParse(parts[1], out var iterations).Should().BeTrue();
            iterations.Should().BeGreaterThan(0);
            Convert.FromBase64String(parts[2]).Should().NotBeEmpty();
            Convert.FromBase64String(parts[3]).Should().NotBeEmpty();
        }

        [Fact]
        public void Hash_SamePasswordTwice_ReturnsDifferentHashes()
        {
            var first = _hasher.Hash("secreta123");
            var second = _hasher.Hash("secreta123");

            first.Should().NotBe(second);
        }

        [Fact]
        public void Verify_CorrectPassword_ReturnsTrue()
        {
            var hash = _hasher.Hash("secreta123");

            _hasher.Verify("secreta123", hash).Should().BeTrue();
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            var hash = _hasher.Hash("secreta123");

            _hasher.Verify("otra-clave", hash).Should().BeFalse();
        }

        [Fact]
        public void Verify_MalformedHash_ReturnsFalse()
        {
            Action act = () => _hasher.Verify("secreta123", "basura").Should().BeFalse();

            act.Should().NotThrow();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Verify_NullOrEmptyPassword_ReturnsFalse(string? password)
        {
            var hash = _hasher.Hash("secreta123");

            _hasher.Verify(password!, hash).Should().BeFalse();
        }

        [Fact]
        public void Hash_NullPassword_ThrowsArgumentException()
        {
            Action act = () => _hasher.Hash(null!);

            act.Should().Throw<ArgumentException>();
        }
    }
}
