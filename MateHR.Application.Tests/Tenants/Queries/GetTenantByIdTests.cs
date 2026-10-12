using FluentAssertions;
using MateHR.Application.Tenants.Queries;
using MateHR.Application.Tests.Common;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.Interfaces;
using Moq;
using Xunit;

namespace MateHR.Application.Tests.Tenants.Queries
{
    public class GetTenantByIdTests
    {
        private static Tenant CreateTenant() => new(
            "Acme Corp",
            "acme-corp",
            "20304050601",
            "owner@acme.com",
            Industry.Technology,
            null,
            TenantStatus.Active,
            RecruitmentMode.Internal,
            SubscriptionType.Free);

        [Fact]
        public async Task ExecuteAsync_ExistingId_ReturnsMappedResponse()
        {
            var tenant = CreateTenant();
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.GetTenantByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);

            var result = await new GetTenantById(repository.Object, TestMapper.Create())
                .ExecuteAsync(tenant.Id);

            result.Id.Should().Be(tenant.Id);
            result.Name.Should().Be(tenant.Name);
            result.Slug.Should().Be(tenant.Slug);
        }

        [Fact]
        public async Task ExecuteAsync_UnknownId_ThrowsKeyNotFoundException()
        {
            var tenantId = Guid.NewGuid();
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.GetTenantByIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException());

            Func<Task> act = () => new GetTenantById(repository.Object, TestMapper.Create())
                .ExecuteAsync(tenantId);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
