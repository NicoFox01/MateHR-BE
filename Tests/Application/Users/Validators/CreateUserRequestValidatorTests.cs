using FluentAssertions;
using MateHR.Application.Users.DTOs;
using MateHR.Application.Users.Validators;
using MateHR.Domain.Users.Enums;
using Xunit;

namespace MateHR.Application.Tests.Users.Validators
{
    public class CreateUserRequestValidatorTests
    {
        private static CreateUserRequest ValidRequest() => new()
        {
            FirstName = "Ana",
            LastName = "Pérez",
            Email = "ana@acme.com",
            Password = "secreta123",
            Role = UserRole.Recruiter,
            TenantId = Guid.NewGuid()
        };

        [Fact]
        public void Validate_SuperAdminWithTenant_IsInvalid()
        {
            var request = ValidRequest();
            request.Role = UserRole.SuperAdmin;
            request.TenantId = Guid.NewGuid();

            var result = new CreateUserRequestValidator().Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.PropertyName == "TenantId" && e.ErrorMessage.Contains("SuperAdmin"));
        }

        [Fact]
        public void Validate_RoleWithoutTenant_IsInvalid()
        {
            var request = ValidRequest();
            request.Role = UserRole.Recruiter;
            request.TenantId = null;

            new CreateUserRequestValidator().Validate(request).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_TenantIdEmpty_IsInvalid()
        {
            var request = ValidRequest();
            request.Role = UserRole.Employee;
            request.TenantId = Guid.Empty;

            var result = new CreateUserRequestValidator().Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "El tenant no puede ser un Guid vacio.");
        }

        [Fact]
        public void Validate_ShortPassword_IsInvalid()
        {
            var request = ValidRequest();
            request.Password = "123";

            new CreateUserRequestValidator().Validate(request).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_InvalidEmail_IsInvalid()
        {
            var request = ValidRequest();
            request.Email = "no-es-un-mail";

            new CreateUserRequestValidator().Validate(request).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_ValidRequest_IsValid()
        {
            var result = new CreateUserRequestValidator().Validate(ValidRequest());

            result.IsValid.Should().BeTrue();
        }
    }
}
