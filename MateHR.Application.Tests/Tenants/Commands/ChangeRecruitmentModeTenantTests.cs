using FluentAssertions;
using FluentValidation;
using MateHR.Application.Tenants.Commands;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Validators;
using MateHR.Application.Tests.Common;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.Interfaces;
using Moq;
using Xunit;

namespace MateHR.Application.Tests.Tenants.Commands
{
    public class ChangeRecruitmentModeTenantTests
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

        private static ChangeRecruitmentModeTenant BuildCommand(Mock<ITenantRepository> repository)
            => new(repository.Object, TestMapper.Create(), new ChangeRecruitmentModeDtoValidator());

        [Fact]
        public async Task ExecuteAsync_ValidDto_UpdatesAndReturnsMappedResponse()
        {
            var tenant = ExistingTenant();
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.UpdateRecruitmentModeTenantAsync(tenant.Id, It.IsAny<RecruitmentMode>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid _, RecruitmentMode mode, CancellationToken _) =>
                {
                    tenant.ChangeRecruitmentMode(mode);
                    return tenant;
                });

            var result = await BuildCommand(repository).ExecuteAsync(tenant.Id, new ChangeRecruitmentModeDto
            {
                RecruitmentMode = RecruitmentMode.External
            });

            result.RecruitmentMode.Should().Be(RecruitmentMode.External);
            repository.Verify(
                r => r.UpdateRecruitmentModeTenantAsync(tenant.Id, RecruitmentMode.External, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_UndefinedEnum_ThrowsValidationException()
        {
            var repository = new Mock<ITenantRepository>();

            Func<Task> act = () => BuildCommand(repository).ExecuteAsync(Guid.NewGuid(), new ChangeRecruitmentModeDto
            {
                RecruitmentMode = (RecruitmentMode)999
            });

            await act.Should().ThrowAsync<ValidationException>();
            repository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_TenantNotFound_ThrowsKeyNotFoundException()
        {
            var tenantId = Guid.NewGuid();
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.UpdateRecruitmentModeTenantAsync(tenantId, It.IsAny<RecruitmentMode>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException());

            Func<Task> act = () => BuildCommand(repository).ExecuteAsync(tenantId, new ChangeRecruitmentModeDto
            {
                RecruitmentMode = RecruitmentMode.External
            });

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
