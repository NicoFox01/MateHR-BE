using FluentAssertions;
using MateHR.Application.Recruitment.JobStatuses.Commands;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Exceptions;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using Moq;

namespace MateHR.Application.Tests.Recruitment.JobStatuses.Commands
{
    public class DeleteJobStatusTests
    {
        private static JobStatus CreateStatus(Guid tenantId, string name = "Activo") => new(tenantId, name);

        private static DeleteJobStatus BuildUseCase(Mock<IJobStatusRepository> repository, Guid? tenantId)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);

            return new DeleteJobStatus(repository.Object, tenantContext.Object);
        }

        // Caso 10: entidad existente, sin referencias.
        [Fact]
        public async Task ExecuteAsync_NotInUse_DeletesOnce()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();
            var entity = CreateStatus(tenantId);
            repository
                .Setup(r => r.GetByIdAsync(tenantId, entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
            repository
                .Setup(r => r.IsInUseAsync(tenantId, entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            await BuildUseCase(repository, tenantId).ExecuteAsync(entity.Id);

            repository.Verify(
                r => r.DeleteAsync(tenantId, entity.Id, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // Caso 11: IsInUseAsync true.
        [Fact]
        public async Task ExecuteAsync_InUse_ThrowsAndDoesNotDelete()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();
            var entity = CreateStatus(tenantId);
            repository
                .Setup(r => r.GetByIdAsync(tenantId, entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
            repository
                .Setup(r => r.IsInUseAsync(tenantId, entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            Func<Task> act = () => BuildUseCase(repository, tenantId).ExecuteAsync(entity.Id);

            await act.Should().ThrowAsync<EntityInUseException>();
            repository.Verify(
                r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // Caso 12: id inexistente.
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
            repository.Verify(
                r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
