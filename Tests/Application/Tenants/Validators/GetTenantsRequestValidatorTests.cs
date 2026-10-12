using FluentAssertions;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Validators;
using Xunit;

namespace MateHR.Application.Tests.Tenants.Validators
{
    public class GetTenantsRequestValidatorTests
    {
        [Fact]
        public void Validate_PageSizeZero_IsInvalid()
        {
            var request = new GetTenantsRequest { PageNumber = 1, PageSize = 0 };

            new GetTenantsRequestValidator().Validate(request).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_PageSizeAbove100_IsInvalid()
        {
            var request = new GetTenantsRequest { PageNumber = 1, PageSize = 101 };

            new GetTenantsRequestValidator().Validate(request).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_PageNumberZero_IsInvalid()
        {
            var request = new GetTenantsRequest { PageNumber = 0, PageSize = 20 };

            new GetTenantsRequestValidator().Validate(request).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_DefaultRequest_IsValid()
        {
            var request = new GetTenantsRequest();

            new GetTenantsRequestValidator().Validate(request).IsValid.Should().BeTrue();
        }
    }
}
