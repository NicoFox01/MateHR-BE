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
    public class GetTenantBySlugTests
    {
        private static Tenant CreateTenant() => new(
            "Mi Empresa",
            "mi-empresa",
            "20304050601",
            "owner@acme.com",
            Industry.Technology,
            null,
            TenantStatus.Active,
            RecruitmentMode.Internal,
            SubscriptionType.Free);

        [Fact]
        public async Task ExecuteAsync_UncleanSlug_NormalizesSlugBeforeQuery()
        {
            var tenant = CreateTenant();
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.GetTenantBySlugAsync("mi-empresa", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tenant);

            var result = await new GetTenantBySlug(repository.Object, TestMapper.Create())
                .ExecuteAsync("Mi Empresa");

            result.Slug.Should().Be("mi-empresa");
            repository.Verify(
                r => r.GetTenantBySlugAsync("mi-empresa", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_UnknownSlug_ThrowsKeyNotFoundException()
        {
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.GetTenantBySlugAsync("no-existe", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException());

            Func<Task> act = () => new GetTenantBySlug(repository.Object, TestMapper.Create())
                .ExecuteAsync("no-existe");

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
