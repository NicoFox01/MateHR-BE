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
    public class CreateAreaTests
    {
        private static CreateArea BuildUseCase(Mock<IAreaRepository> repository, Guid? tenantId)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);

            return new CreateArea(
                repository.Object,
                tenantContext.Object,
                TestMapper.Create(),
                new CreateAreaDtoValidator());
        }

        [Fact]
        public async Task ExecuteAsync_ValidDto_CreatesOnceWithTenantAndName()
        {
            var repository = new Mock<IAreaRepository>();
            repository
                .Setup(r => r.CreateAsync(It.IsAny<Area>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Area area, CancellationToken _) => area);
            var tenantId = Guid.NewGuid();

            var result = await BuildUseCase(repository, tenantId)
                .ExecuteAsync(new CreateAreaDto { Name = "Ventas" });

            result.Id.Should().NotBeEmpty();
            result.Name.Should().Be("Ventas");
            result.TenantId.Should().Be(tenantId);
            result.IsActive.Should().BeTrue();
            repository.Verify(
                r => r.CreateAsync(
                    It.Is<Area>(a => a.TenantId == tenantId && a.Name == "Ventas"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_NameWithSpaces_CreatesWithTrimmedName()
        {
            var repository = new Mock<IAreaRepository>();
            repository
                .Setup(r => r.CreateAsync(It.IsAny<Area>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Area area, CancellationToken _) => area);
            var tenantId = Guid.NewGuid();

            var result = await BuildUseCase(repository, tenantId)
                .ExecuteAsync(new CreateAreaDto { Name = "  Ventas  " });

            result.Name.Should().Be("Ventas");
            repository.Verify(
                r => r.NameExistsAsync(tenantId, "Ventas", null, It.IsAny<CancellationToken>()),
                Times.Once);
            repository.Verify(
                r => r.CreateAsync(
                    It.Is<Area>(a => a.TenantId == tenantId && a.Name == "Ventas"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_DuplicateName_ThrowsAndDoesNotCreate()
        {
            var repository = new Mock<IAreaRepository>();
            repository
                .Setup(r => r.NameExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var tenantId = Guid.NewGuid();

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(new CreateAreaDto { Name = "Ventas" });

            await act.Should().ThrowAsync<DuplicateEntityNameException>();
            repository.Verify(
                r => r.CreateAsync(It.IsAny<Area>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_NullTenant_ThrowsUnauthorizedAndSkipsRepository()
        {
            var repository = new Mock<IAreaRepository>();

            Func<Task> act = () => BuildUseCase(repository, null)
                .ExecuteAsync(new CreateAreaDto { Name = "Ventas" });

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            repository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_EmptyName_ThrowsValidationAndSkipsRepository()
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(new CreateAreaDto { Name = "" });

            await act.Should().ThrowAsync<ValidationException>();
            repository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_NameTooLong_ThrowsValidationAndSkipsRepository()
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(new CreateAreaDto { Name = new string('A', Area.NameMaxLength + 1) });

            await act.Should().ThrowAsync<ValidationException>();
            repository.VerifyNoOtherCalls();
        }
    }
}
