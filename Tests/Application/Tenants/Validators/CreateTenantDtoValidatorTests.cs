using FluentAssertions;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Validators;
using MateHR.Domain.Tenants.Enums;
using Xunit;

namespace MateHR.Application.Tests.Tenants.Validators
{
    public class CreateTenantDtoValidatorTests
    {
        private static CreateTenantDto ValidDto() => new()
        {
            Name = "Acme Corp",
            CUIT = "20304050601",
            OwnerEmail = "owner@acme.com",
            Industry = Industry.Technology,
            Address = null
        };

        [Fact]
        public void Validate_EmptyName_IsInvalid()
        {
            var dto = ValidDto();
            dto.Name = "";

            var result = new CreateTenantDtoValidator().Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }

        [Fact]
        public void Validate_NameExceedsMaxLength_IsInvalid()
        {
            var dto = ValidDto();
            dto.Name = new string('a', 201);

            new CreateTenantDtoValidator().Validate(dto).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_CuitWithLetters_IsInvalid()
        {
            var dto = ValidDto();
            dto.CUIT = "20A30405061";

            new CreateTenantDtoValidator().Validate(dto).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_CuitWrongLength_IsInvalid()
        {
            var dto = ValidDto();
            dto.CUIT = "123";

            new CreateTenantDtoValidator().Validate(dto).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_InvalidOwnerEmail_IsInvalid()
        {
            var dto = ValidDto();
            dto.OwnerEmail = "no-es-un-mail";

            new CreateTenantDtoValidator().Validate(dto).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_NullIndustry_IsInvalid()
        {
            var dto = ValidDto();
            dto.Industry = null;

            new CreateTenantDtoValidator().Validate(dto).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_ValidDto_IsValid()
        {
            var result = new CreateTenantDtoValidator().Validate(ValidDto());

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
    }
}
