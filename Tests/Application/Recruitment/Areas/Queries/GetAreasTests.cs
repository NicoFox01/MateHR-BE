using FluentAssertions;
using FluentValidation;
using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Application.Recruitment.Areas.Queries;
using MateHR.Application.Recruitment.Areas.Validators;
using MateHR.Application.Tests.Common;
using MateHR.Domain.Common;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using Moq;

namespace MateHR.Application.Tests.Recruitment.Areas.Queries
{
    public class GetAreasTests
    {
        private static Area CreateArea(Guid tenantId, string name) => new(tenantId, name);

        private static GetAreas BuildUseCase(Mock<IAreaRepository> repository, Guid? tenantId)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);

            return new GetAreas(
                repository.Object,
                tenantContext.Object,
                TestMapper.Create(),
                new GetAreasRequestValidator());
        }

        [Fact]
        public async Task ExecuteAsync_ForwardsPagingToRepositoryAndMapsPagedResult()
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();
            var items = new List<Area>
            {
                CreateArea(tenantId, "Comercial"),
                CreateArea(tenantId, "Marketing"),
                CreateArea(tenantId, "Producto"),
                CreateArea(tenantId, "RRHH"),
                CreateArea(tenantId, "Ventas")
            };
            repository
                .Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<Area>(items, 2, 10, 15));

            var result = await BuildUseCase(repository, tenantId)
                .ExecuteAsync(new GetAreasRequest { PageNumber = 2, PageSize = 10 });

            repository.Verify(
                r => r.GetAllAsync(tenantId, 2, 10, false, It.IsAny<CancellationToken>()),
                Times.Once);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(15);
            result.Items.Should().HaveCount(5);
        }

        [Theory]
        [InlineData(0, 20)]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        public async Task ExecuteAsync_InvalidPaging_ThrowsValidationAndSkipsRepository(int pageNumber, int pageSize)
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(new GetAreasRequest { PageNumber = pageNumber, PageSize = pageSize });

            await act.Should().ThrowAsync<ValidationException>();
            repository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_TwoEntities_ReturnsMappedItemsInNameOrder()
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();
            var items = new List<Area> { CreateArea(tenantId, "Comercial"), CreateArea(tenantId, "Ventas") };
            repository
                .Setup(r => r.GetAllAsync(tenantId, 1, 20, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<Area>(items, 1, 20, 2));

            var result = await BuildUseCase(repository, tenantId).ExecuteAsync(new GetAreasRequest());

            result.Items.Should().HaveCount(2);
            result.Items.Select(a => a.Name).Should().ContainInOrder("Comercial", "Ventas");
            result.Items.Should().OnlyContain(a => a.TenantId == tenantId);
        }

        [Fact]
        public async Task ExecuteAsync_IncludeInactiveTrue_ForwardsFlagToRepository()
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();
            repository
                .Setup(r => r.GetAllAsync(tenantId, 1, 20, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<Area>(new List<Area>(), 1, 20, 0));

            await BuildUseCase(repository, tenantId)
                .ExecuteAsync(new GetAreasRequest { IncludeInactive = true });

            repository.Verify(
                r => r.GetAllAsync(tenantId, 1, 20, true, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
