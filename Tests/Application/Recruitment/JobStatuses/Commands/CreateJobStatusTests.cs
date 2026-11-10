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
    public class CreateJobStatusTests
    {
        private static CreateJobStatus BuildUseCase(Mock<IJobStatusRepository> repository, Guid? tenantId)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);

            return new CreateJobStatus(
                repository.Object,
                tenantContext.Object,
                TestMapper.Create(),
                new CreateJobStatusDtoValidator());
        }

        // Caso 1: DTO {Name:"Abierto"}, tenant A, no duplicado.
        [Fact]
        public async Task ExecuteAsync_ValidDto_CreatesOnceWithTenantAndName()
        {
            var repository = new Mock<IJobStatusRepository>();
            repository
                .Setup(r => r.CreateAsync(It.IsAny<JobStatus>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((JobStatus status, CancellationToken _) => status);
            var tenantId = Guid.NewGuid();

            var result = await BuildUseCase(repository, tenantId)
                .ExecuteAsync(new CreateJobStatusDto { Name = "Abierto" });

            result.Id.Should().NotBeEmpty();
            result.Name.Should().Be("Abierto");
            result.TenantId.Should().Be(tenantId);
            repository.Verify(
                r => r.CreateAsync(
                    It.Is<JobStatus>(s => s.TenantId == tenantId && s.Name == "Abierto"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // Caso 2: DTO {Name:"  Abierto  "}, se crea trimmeado.
        [Fact]
        public async Task ExecuteAsync_NameWithSpaces_CreatesWithTrimmedName()
        {
            var repository = new Mock<IJobStatusRepository>();
            repository
                .Setup(r => r.CreateAsync(It.IsAny<JobStatus>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((JobStatus status, CancellationToken _) => status);
            var tenantId = Guid.NewGuid();

            var result = await BuildUseCase(repository, tenantId)
                .ExecuteAsync(new CreateJobStatusDto { Name = "  Abierto  " });

            result.Name.Should().Be("Abierto");
            repository.Verify(
                r => r.NameExistsAsync(tenantId, "Abierto", null, It.IsAny<CancellationToken>()),
                Times.Once);
            repository.Verify(
                r => r.CreateAsync(
                    It.Is<JobStatus>(s => s.TenantId == tenantId && s.Name == "Abierto"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // Caso 3: DTO válido, NameExistsAsync true.
        [Fact]
        public async Task ExecuteAsync_DuplicateName_ThrowsAndDoesNotCreate()
        {
            var repository = new Mock<IJobStatusRepository>();
            repository
                .Setup(r => r.NameExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var tenantId = Guid.NewGuid();

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(new CreateJobStatusDto { Name = "Abierto" });

            await act.Should().ThrowAsync<DuplicateEntityNameException>();
            repository.Verify(
                r => r.CreateAsync(It.IsAny<JobStatus>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // Caso 4: DTO válido, ITenantContext nulo.
        [Fact]
        public async Task ExecuteAsync_NullTenant_ThrowsUnauthorizedAndSkipsRepository()
        {
            var repository = new Mock<IJobStatusRepository>();

            Func<Task> act = () => BuildUseCase(repository, null)
                .ExecuteAsync(new CreateJobStatusDto { Name = "Abierto" });

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            repository.VerifyNoOtherCalls();
        }

        // Caso 5: Name vacío.
        [Fact]
        public async Task ExecuteAsync_EmptyName_ThrowsValidationAndSkipsRepository()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(new CreateJobStatusDto { Name = "" });

            await act.Should().ThrowAsync<ValidationException>();
            repository.VerifyNoOtherCalls();
        }

        // Caso 6: Name de NameMaxLength + 1 caracteres (30 + 1, D-decision: máx queda en 30).
        [Fact]
        public async Task ExecuteAsync_NameTooLong_ThrowsValidationAndSkipsRepository()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();

            Func<Task> act = () => BuildUseCase(repository, tenantId)
                .ExecuteAsync(new CreateJobStatusDto { Name = new string('A', JobStatus.NameMaxLength + 1) });

            await act.Should().ThrowAsync<ValidationException>();
            repository.VerifyNoOtherCalls();
        }
    }
}
