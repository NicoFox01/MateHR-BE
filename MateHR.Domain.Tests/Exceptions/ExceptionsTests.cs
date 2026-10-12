using FluentAssertions;
using MateHR.Domain.Exceptions;
using Xunit;

namespace MateHR.Domain.Tests.Exceptions
{
    public class ExceptionsTests
    {
        [Fact]
        public void SlugAlreadyExistsException_Slug_ContainsSlugAndExposesProperty()
        {
            var exception = new SlugAlreadyExistsException("acme-corp");

            exception.Message.Should().Contain("acme-corp");
            exception.Slug.Should().Be("acme-corp");
        }

        [Fact]
        public void EmailAlreadyExistsException_Email_ContainsEmailAndExposesProperty()
        {
            var exception = new EmailAlreadyExistsException("ana@acme.com");

            exception.Message.Should().Contain("ana@acme.com");
            exception.Email.Should().Be("ana@acme.com");
        }

        [Fact]
        public void InvalidCredentialsException_DefaultMessage_ReturnsDefaultText()
        {
            var exception = new InvalidCredentialsException();

            exception.Message.Should().Be("Las credenciales son invalidas.");
        }

        [Fact]
        public void InvalidCredentialsException_CustomMessage_ReturnsCustomMessage()
        {
            var exception = new InvalidCredentialsException("El usuario esta inactivo.");

            exception.Message.Should().Be("El usuario esta inactivo.");
        }
    }
}
