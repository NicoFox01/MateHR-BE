using FluentAssertions;
using FluentValidation;
using MateHR.Application.Recruitment.JobStatuses.DTOs;
using MateHR.Application.Recruitment.JobStatuses.Queries;
using MateHR.Application.Recruitment.JobStatuses.Validators;
using MateHR.Application.Tests.Common;
using MateHR.Domain.Common;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using Moq;

namespace MateHR.Application.Tests.Recruitment.JobStatuses.Queries
{
    public class GetJobStatusesTests
    {
        private static JobStatus CreateStatus(Guid tenantId, string name) => new(tenantId, name);

        private static GetJobStatuses BuildUseCase(Mock<IJobStatusRepository> repository, Guid? tenantId)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);

            return new GetJobStatuses(
                repository.Object,
                tenantContext.Object,
                TestMapper.Create(),
                new GetJobStatusesRequestValidator());
        }

        // Caso 15: tenant A, página 2, tamaño 10.
        [Fact]
        public async Task ExecuteAsync_ForwardsPagingToRepositoryAndMapsPagedResult()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();
            var items = new List<JobStatus>
            {
                CreateStatus(tenantId, "Abierto"),
                CreateStatus(tenantId, "Cerrado"),
                CreateStatus(tenantId, "En proceso"),
                CreateStatus(tenantId, "Pausado"),
                CreateStatus(tenantId, "Nuevo")
            };
            repository
                .Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<JobStatus>(items, 2, 10, 15));

            var result = await BuildUseCase(repository, tenantId)
                .ExecuteAsync(new GetJobStatusesRequest { PageNumber = 2, PageSize = 10 });

            repository.Verify(
                r => r.GetAllAsync(tenantId, 2, 10, It.IsAny<CancellationToken>()),
                Times.Once);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(15);
            result.Items.Should().HaveCount(5);
        }

        // Caso 16: página 0, tamaño 0 o tamaño 101.
        [Theory]
        [InlineData(0, 20)]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        public async Task ExecuteAsync_InvalidPaging_ThrowsValidationAndSkipsRepository(int pageNumber, int pageSize)
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(new GetJobStatusesRequest { PageNumber = pageNumber, PageSize = pageSize });

            await act.Should().ThrowAsync<ValidationException>();
            repository.VerifyNoOtherCalls();
        }

        // Caso 17: dos entidades en el tenant, orden por nombre ascendente.
        [Fact]
        public async Task ExecuteAsync_TwoEntities_ReturnsMappedItemsInNameOrder()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();
            var items = new List<JobStatus> { CreateStatus(tenantId, "Abierto"), CreateStatus(tenantId, "Cerrado") };
            repository
                .Setup(r => r.GetAllAsync(tenantId, 1, 20, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<JobStatus>(items, 1, 20, 2));

            var result = await BuildUseCase(repository, tenantId).ExecuteAsync(new GetJobStatusesRequest());

            result.Items.Should().HaveCount(2);
            result.Items.Select(s => s.Name).Should().ContainInOrder("Abierto", "Cerrado");
            result.Items.Should().OnlyContain(s => s.TenantId == tenantId);
        }
    }
}
