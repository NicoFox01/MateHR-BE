using FluentAssertions;
using FluentValidation;
using MateHR.Application.Recruitment.Areas.Commands;
using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Application.Recruitment.Areas.Validators;
using MateHR.Application.Tests.Common;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Exceptions;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using Moq;

namespace MateHR.Application.Tests.Recruitment.Areas.Commands
{
    public class UpdateAreaTests
    {
        private static Area CreateArea(Guid tenantId, string name = "Ventas") => new(tenantId, name);

        private static UpdateArea BuildUseCase(Mock<IAreaRepository> repository, Guid? tenantId)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);

            return new UpdateArea(
                repository.Object,
                tenantContext.Object,
                TestMapper.Create(),
                new UpdateAreaDtoValidator());
        }

        [Fact]
        public async Task ExecuteAsync_ExistingWithoutDuplicate_RenamesWithTenantIdAndName()
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();
            var entity = CreateArea(tenantId, "Ventas");
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
                .ExecuteAsync(entity.Id, new UpdateAreaDto { Name = "Comercial" });

            result.Id.Should().Be(entity.Id);
            result.TenantId.Should().Be(tenantId);
            result.Name.Should().Be("Comercial");
            repository.Verify(
                r => r.RenameAsync(tenantId, entity.Id, "Comercial", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_DuplicateName_ThrowsAndDoesNotRename()
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();
            var entity = CreateArea(tenantId, "Ventas");
            repository
                .Setup(r => r.GetByIdAsync(tenantId, entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
            repository
                .Setup(r => r.NameExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(entity.Id, new UpdateAreaDto { Name = "Marketing" });

            await act.Should().ThrowAsync<DuplicateEntityNameException>();
            repository.Verify(
                r => r.NameExistsAsync(tenantId, "Marketing", entity.Id, It.IsAny<CancellationToken>()),
                Times.Once);
            repository.Verify(
                r => r.RenameAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
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

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(unknownId, new UpdateAreaDto { Name = "Comercial" });

            await act.Should().ThrowAsync<KeyNotFoundException>();
            repository.Verify(
                r => r.RenameAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
