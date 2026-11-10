using FluentAssertions;
using FluentValidation;
using MateHR.Application.Recruitment.JobStatuses.Commands;
using MateHR.Application.Recruitment.JobStatuses.DTOs;
using MateHR.Application.Recruitment.JobStatuses.Validators;
using MateHR.Application.Tests.Common;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Exceptions;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using Moq;

namespace MateHR.Application.Tests.Recruitment.JobStatuses.Commands
{
    public class UpdateJobStatusTests
    {
        private static JobStatus CreateStatus(Guid tenantId, string name = "Activo") => new(tenantId, name);

        private static UpdateJobStatus BuildUseCase(Mock<IJobStatusRepository> repository, Guid? tenantId)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);

            return new UpdateJobStatus(
                repository.Object,
                tenantContext.Object,
                TestMapper.Create(),
                new UpdateJobStatusDtoValidator());
        }

        // Caso 7: entidad existente, DTO {Name:"Pausado"}, sin duplicado.
        [Fact]
        public async Task ExecuteAsync_ExistingWithoutDuplicate_RenamesWithTenantIdAndName()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();
            var entity = CreateStatus(tenantId, "Activo");
            repository
                .Setup(r => r.GetByIdAsync(tenantId, entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
            repository
                .Setup(r => r.RenameAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid _, Guid _, string name, CancellationToken _) =>
                {
                    entity.Rename(name);
                    return entity;
                });

            var result = await BuildUseCase(repository, tenantId)
                .ExecuteAsync(entity.Id, new UpdateJobStatusDto { Name = "Pausado" });

            result.Id.Should().Be(entity.Id);
            result.TenantId.Should().Be(tenantId);
            result.Name.Should().Be("Pausado");
            repository.Verify(
                r => r.RenameAsync(tenantId, entity.Id, "Pausado", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // Caso 8: entidad existente, nombre de otro estado del tenant.
        [Fact]
        public async Task ExecuteAsync_DuplicateName_ThrowsAndDoesNotRename()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();
            var entity = CreateStatus(tenantId, "Activo");
            repository
                .Setup(r => r.GetByIdAsync(tenantId, entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
            repository
                .Setup(r => r.NameExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(entity.Id, new UpdateJobStatusDto { Name = "Cerrado" });

            await act.Should().ThrowAsync<DuplicateEntityNameException>();
            repository.Verify(
                r => r.NameExistsAsync(tenantId, "Cerrado", entity.Id, It.IsAny<CancellationToken>()),
                Times.Once);
            repository.Verify(
                r => r.RenameAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // Caso 9: id inexistente.
        [Fact]
        public async Task ExecuteAsync_UnknownId_ThrowsKeyNotFoundException()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();
            var unknownId = Guid.NewGuid();
            repository
                .Setup(r => r.GetByIdAsync(tenantId, unknownId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException($"No se encontro el estado con ID {unknownId}"));

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(unknownId, new UpdateJobStatusDto { Name = "Pausado" });

            await act.Should().ThrowAsync<KeyNotFoundException>();
            repository.Verify(
                r => r.RenameAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
