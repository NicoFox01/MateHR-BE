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
    public class UpdateTenantTests
    {
        private static Tenant ExistingTenant() => new(
            "Acme Corp",
            "acme-corp",
            "20304050601",
            "owner@acme.com",
            Industry.Technology,
            null,
            TenantStatus.Active,
            RecruitmentMode.Internal,
            SubscriptionType.Free);

        private static UpdateTenantDto ValidDto() => new()
        {
            Name = "Nuevo Slug",
            CUIT = "11222333444",
            OwnerEmail = "nuevo@empresa.com",
            Industry = Industry.Finance,
            Address = null
        };

        private static UpdateTenant BuildCommand(Mock<ITenantRepository> repository)
            => new(repository.Object, TestMapper.Create(), new UpdateTenantDtoValidator());

        [Fact]
        public async Task ExecuteAsync_ValidDto_UpdatesTenantAndReturnsMappedResponse()
        {
            var tenant = ExistingTenant();
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.GetTenantByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
            repository
                .Setup(r => r.ExistsBySlugAsync("nuevo-slug", tenant.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            repository
                .Setup(r => r.UpdateTenantAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant t, CancellationToken _) => t);

            var result = await BuildCommand(repository).ExecuteAsync(tenant.Id, ValidDto());

            result.Name.Should().Be("Nuevo Slug");
            result.Slug.Should().Be("nuevo-slug");
            repository.Verify(
                r => r.UpdateTenantAsync(
                    It.Is<Tenant>(t => t.Slug == "nuevo-slug"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_DuplicateSlug_ThrowsAndExcludesOwnTenant()
        {
            var tenant = ExistingTenant();
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.GetTenantByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);
            repository
                .Setup(r => r.ExistsBySlugAsync("nuevo-slug", tenant.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            Func<Task> act = () => BuildCommand(repository).ExecuteAsync(tenant.Id, ValidDto());

            await act.Should().ThrowAsync<SlugAlreadyExistsException>();
            repository.Verify(
                r => r.ExistsBySlugAsync("nuevo-slug", tenant.Id, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_TenantNotFound_ThrowsKeyNotFoundException()
        {
            var tenantId = Guid.NewGuid();
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException("No se encontro el tenant"));

            Func<Task> act = () => BuildCommand(repository).ExecuteAsync(tenantId, ValidDto());

            await act.Should().ThrowAsync<KeyNotFoundException>();
            repository.Verify(
                r => r.ExistsBySlugAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_InvalidDto_ThrowsValidationException()
        {
            var repository = new Mock<ITenantRepository>();
            var dto = ValidDto();
            dto.Name = "";

            Func<Task> act = () => BuildCommand(repository).ExecuteAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<ValidationException>();
            repository.VerifyNoOtherCalls();
        }
    }
}
