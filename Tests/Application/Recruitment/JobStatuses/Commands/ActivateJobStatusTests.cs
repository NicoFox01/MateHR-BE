using FluentAssertions;
using MateHR.Application.Recruitment.JobStatuses.Commands;
using MateHR.Application.Tests.Common;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using Moq;

namespace MateHR.Application.Tests.Recruitment.JobStatuses.Commands
{
    public class ActivateJobStatusTests
    {
        private static JobStatus CreateStatus(Guid tenantId, string name = "Activo") => new(tenantId, name);

        private static ActivateJobStatus BuildUseCase(Mock<IJobStatusRepository> repository, Guid? tenantId)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);

            return new ActivateJobStatus(repository.Object, tenantContext.Object, TestMapper.Create());
        }

        [Fact]
        public async Task ExecuteAsync_ActivatesAndMapsResponse()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();
            var entity = CreateStatus(tenantId);
            entity.Deactivate();
            repository
                .Setup(r => r.SetActiveAsync(tenantId, entity.Id, true, It.IsAny<CancellationToken>()))
                .Callback<Guid, Guid, bool, CancellationToken>((_, _, isActive, _) =>
                {
                    if (isActive) entity.Activate(); else entity.Deactivate();
                })
                .ReturnsAsync(entity);

            var result = await BuildUseCase(repository, tenantId).ExecuteAsync(entity.Id);

            repository.Verify(
                r => r.SetActiveAsync(tenantId, entity.Id, true, It.IsAny<CancellationToken>()),
                Times.Once);
            result.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_NoTenant_ThrowsUnauthorizedAndSkipsRepository()
        {
            var repository = new Mock<IJobStatusRepository>();

            Func<Task> act = () => BuildUseCase(repository, null).ExecuteAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            repository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_UnknownId_ThrowsKeyNotFoundException()
        {
            var repository = new Mock<IJobStatusRepository>();
            var tenantId = Guid.NewGuid();
            var unknownId = Guid.NewGuid();
            repository
                .Setup(r => r.SetActiveAsync(tenantId, unknownId, true, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException($"No se encontro la entidad con ID: {unknownId}"));

            Func<Task> act = () => BuildUseCase(repository, tenantId).ExecuteAsync(unknownId);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
