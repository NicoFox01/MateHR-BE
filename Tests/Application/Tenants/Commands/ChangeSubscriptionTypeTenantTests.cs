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
    public class ChangeSubscriptionTypeTenantTests
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

        private static ChangeSubscriptionTypeTenant BuildCommand(Mock<ITenantRepository> repository)
            => new(repository.Object, TestMapper.Create(), new ChangeSubscriptionTypeDtoValidator());

        [Fact]
        public async Task ExecuteAsync_ValidDto_UpdatesAndReturnsMappedResponse()
        {
            var tenant = ExistingTenant();
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.UpdateSubscriptionTypeTenantAsync(tenant.Id, It.IsAny<SubscriptionType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid _, SubscriptionType type, CancellationToken _) =>
                {
                    tenant.ChangeSubscriptionType(type);
                    return tenant;
                });

            var result = await BuildCommand(repository).ExecuteAsync(tenant.Id, new ChangeSubscriptionTypeDto
            {
                SubscriptionType = SubscriptionType.Yearly
            });

            result.SubscriptionType.Should().Be(SubscriptionType.Yearly);
            repository.Verify(
                r => r.UpdateSubscriptionTypeTenantAsync(tenant.Id, SubscriptionType.Yearly, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_UndefinedEnum_ThrowsValidationException()
        {
            var repository = new Mock<ITenantRepository>();

            Func<Task> act = () => BuildCommand(repository).ExecuteAsync(Guid.NewGuid(), new ChangeSubscriptionTypeDto
            {
                SubscriptionType = (SubscriptionType)999
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
                .Setup(r => r.UpdateSubscriptionTypeTenantAsync(tenantId, It.IsAny<SubscriptionType>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException());

            Func<Task> act = () => BuildCommand(repository).ExecuteAsync(tenantId, new ChangeSubscriptionTypeDto
            {
                SubscriptionType = SubscriptionType.Yearly
            });

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
