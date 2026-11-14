using FluentAssertions;
using MateHR.Application.Recruitment.Areas.Commands;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Exceptions;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using Moq;

namespace MateHR.Application.Tests.Recruitment.Areas.Commands
{
    public class DeleteAreaTests
    {
        private static Area CreateArea(Guid tenantId, string name = "Ventas") => new(tenantId, name);

        private static DeleteArea BuildUseCase(Mock<IAreaRepository> repository, Guid? tenantId)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);

            return new DeleteArea(repository.Object, tenantContext.Object);
        }

        [Fact]
        public async Task ExecuteAsync_NotInUse_DeletesOnce()
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();
            var entity = CreateArea(tenantId);
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

        [Fact]
        public async Task ExecuteAsync_InUse_ThrowsAndDoesNotDelete()
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();
            var entity = CreateArea(tenantId);
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

        [Fact]
        public async Task ExecuteAsync_UnknownId_ThrowsKeyNotFoundException()
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();
            var unknownId = Guid.NewGuid();
            repository
                .Setup(r => r.GetByIdAsync(tenantId, unknownId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException($"No se encontro la entidad con ID: {unknownId}"));

            Func<Task> act = () => BuildUseCase(repository, tenantId).ExecuteAsync(unknownId);

            await act.Should().ThrowAsync<KeyNotFoundException>();
            repository.Verify(
                r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
