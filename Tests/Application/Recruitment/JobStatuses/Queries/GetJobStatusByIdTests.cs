using FluentAssertions;
using MateHR.Application.Recruitment.JobStatuses.Queries;
using MateHR.Application.Tests.Common;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using Moq;

namespace MateHR.Application.Tests.Recruitment.JobStatuses.Queries
{
    public class GetJobStatusByIdTests
    {
        private static JobStatus CreateStatus(Guid tenantId, string name = "Activo") => new(tenantId, name);

        private static GetJobStatusById BuildUseCase(Mock<IJobStatusRepository> repository, Guid? tenantId)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);

            return new GetJobStatusById(repository.Object, tenantContext.Object, TestMapper.Create());
        }

        // Caso 13: entidad existente.
        [Fact]
        public async Task ExecuteAsync_Existing_ReturnsMappedResponse()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();
            var entity = CreateStatus(tenantId, "Abierto");
            repository
                .Setup(r => r.GetByIdAsync(tenantId, entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            var result = await BuildUseCase(repository, tenantId).ExecuteAsync(entity.Id);

            result.Id.Should().Be(entity.Id);
            result.TenantId.Should().Be(tenantId);
            result.Name.Should().Be("Abierto");
        }

        // Caso 14: id inexistente.
        [Fact]
        public async Task ExecuteAsync_UnknownId_ThrowsKeyNotFoundException()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();
            var unknownId = Guid.NewGuid();
            repository
                .Setup(r => r.GetByIdAsync(tenantId, unknownId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException($"No se encontro el estado con ID {unknownId}"));

            Func<Task> act = () => BuildUseCase(repository, tenantId).ExecuteAsync(unknownId);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
