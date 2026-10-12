using FluentAssertions;
using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Validators;
using Xunit;

namespace MateHR.Application.Tests.Users.Validators
{
    public class LoginRequestValidatorTests
    {
        [Fact]
        public void Validate_InvalidEmail_IsInvalid()
        {
            var request = new LoginRequest { Email = "no-es-un-mail", Password = "secreta123" };

            new LoginRequestValidator().Validate(request).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_ShortPassword_IsInvalid()
        {
            var request = new LoginRequest { Email = "ana@acme.com", Password = "1234567" };

            new LoginRequestValidator().Validate(request).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_ValidRequest_IsValid()
        {
            var request = new LoginRequest { Email = "ana@acme.com", Password = "secreta123" };

            new LoginRequestValidator().Validate(request).IsValid.Should().BeTrue();
        }
    }
}
