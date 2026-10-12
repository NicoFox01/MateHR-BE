using FluentAssertions;
using FluentValidation;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Queries;
using MateHR.Application.Tenants.Validators;
using MateHR.Application.Tests.Common;
using MateHR.Domain.Common;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.Interfaces;
using Moq;
using Xunit;

namespace MateHR.Application.Tests.Tenants.Queries
{
    public class GetTenantsTests
    {
        private static Tenant CreateTenant(string name, string slug) => new(
            name,
            slug,
            "20304050601",
            "owner@acme.com",
            Industry.Technology,
            null,
            TenantStatus.Active,
            RecruitmentMode.Internal,
            SubscriptionType.Free);

        private static GetTenants BuildQuery(Mock<ITenantRepository> repository)
            => new(repository.Object, TestMapper.Create(), new GetTenantsRequestValidator());

        [Fact]
        public async Task ExecuteAsync_ValidRequest_ReturnsPagedResultWithMappedItems()
        {
            var tenants = new List<Tenant> { CreateTenant("Acme", "acme"), CreateTenant("Globex", "globex") };
            var paged = new PagedResult<Tenant>(tenants, 1, 20, 2);
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.GetTenantsAsync(null, 1, 20, It.IsAny<CancellationToken>()))
                .ReturnsAsync(paged);

            var result = await BuildQuery(repository).ExecuteAsync(new GetTenantsRequest
            {
                PageNumber = 1,
                PageSize = 20,
                Status = null
            });

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task ExecuteAsync_ForwardsStatusAndPagingToRepository()
        {
            var repository = new Mock<ITenantRepository>();
            repository
                .Setup(r => r.GetTenantsAsync(It.IsAny<TenantStatus?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<Tenant>(new List<Tenant>(), 3, 50, 0));

            await BuildQuery(repository).ExecuteAsync(new GetTenantsRequest
            {
                Status = TenantStatus.Active,
                PageNumber = 3,
                PageSize = 50
            });

            repository.Verify(
                r => r.GetTenantsAsync(TenantStatus.Active, 3, 50, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_PageSizeAbove100_ThrowsValidationException()
        {
            var repository = new Mock<ITenantRepository>();

            Func<Task> act = () => BuildQuery(repository).ExecuteAsync(new GetTenantsRequest
            {
                PageNumber = 1,
                PageSize = 101
            });

            await act.Should().ThrowAsync<ValidationException>();
            repository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_PageNumberZero_ThrowsValidationException()
        {
            var repository = new Mock<ITenantRepository>();

            Func<Task> act = () => BuildQuery(repository).ExecuteAsync(new GetTenantsRequest
            {
                PageNumber = 0,
                PageSize = 20
            });

            await act.Should().ThrowAsync<ValidationException>();
        }
    }
}
