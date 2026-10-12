using FluentAssertions;
using FluentValidation;
using MateHR.Application.Tenants.Commands;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Validators;
using MateHR.Application.Tests.Common;
using MateHR.Domain.Exceptions;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.Interfaces;
using Moq;
using Xunit;

namespace MateHR.Application.Tests.Tenants.Commands
{
    public class CreateTenantTests
    {
        private static CreateTenantDto ValidDto() => new()
        {
            Name = "Acme Corp",
            CUIT = "20304050601",
            OwnerEmail = "owner@acme.com",
            Industry = Industry.Technology,
            Address = null
        };

        private static CreateTenant BuildCommand(Mock<ITenantRepository> repository)
            => new(repository.Object, TestMapper.Create(), new CreateTenantDtoValidator());

        [Fact]
        public async Task ExecuteAsync_ValidDto_CreatesTenantWithGeneratedSlug()
        {
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.ExistsBySlugAsync("acme-corp", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            repository
                .Setup(r => r.CreateTenantAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant tenant, CancellationToken _) => tenant);

            var result = await BuildCommand(repository).ExecuteAsync(ValidDto());

            result.Name.Should().Be("Acme Corp");
            result.Slug.Should().Be("acme-corp");
            repository.Verify(
                r => r.CreateTenantAsync(
                    It.Is<Tenant>(t =>
                        t.Slug == "acme-corp"
                        && t.Status == TenantStatus.Active
                        && t.RecruitmentMode == RecruitmentMode.Internal
                        && t.SubscriptionType == SubscriptionType.Free),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_DuplicateSlug_ThrowsSlugAlreadyExistsException()
        {
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.ExistsBySlugAsync("acme-corp", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            Action act = () => BuildCommand(repository).ExecuteAsync(ValidDto()).GetAwaiter().GetResult();

            act.Should().Throw<SlugAlreadyExistsException>().Which.Slug.Should().Be("acme-corp");
            repository.Verify(
                r => r.CreateTenantAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_EmptyName_ThrowsValidationException()
        {
            var repository = new Mock<ITenantRepository>();
            var dto = ValidDto();
            dto.Name = "";

            Func<Task> act = () => BuildCommand(repository).ExecuteAsync(dto);

            await act.Should().ThrowAsync<ValidationException>();
            repository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_NullIndustry_ThrowsValidationException()
        {
            var repository = new Mock<ITenantRepository>();
            var dto = ValidDto();
            dto.Industry = null;

            Func<Task> act = () => BuildCommand(repository).ExecuteAsync(dto);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task ExecuteAsync_InvalidCuit_ThrowsValidationException()
        {
            var repository = new Mock<ITenantRepository>();
            var dto = ValidDto();
            dto.CUIT = "20-30405060-1";

            Func<Task> act = () => BuildCommand(repository).ExecuteAsync(dto);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task ExecuteAsync_NameWithSpaces_TrimsNameBeforeCreating()
        {
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.ExistsBySlugAsync("acme-corp", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            repository
                .Setup(r => r.CreateTenantAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant tenant, CancellationToken _) => tenant);

            var dto = ValidDto();
            dto.Name = "  Acme Corp  ";

            await BuildCommand(repository).ExecuteAsync(dto);

            repository.Verify(
                r => r.CreateTenantAsync(
                    It.Is<Tenant>(t => t.Name == "Acme Corp" && t.Slug == "acme-corp"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
