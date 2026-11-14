using FluentAssertions;
using MateHR.Application.Recruitment.Areas.Queries;
using MateHR.Application.Tests.Common;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;
using Moq;

namespace MateHR.Application.Tests.Recruitment.Areas.Queries
{
    public class GetAreaByIdTests
    {
        private static Area CreateArea(Guid tenantId, string name = "Ventas") => new(tenantId, name);

        private static GetAreaById BuildUseCase(Mock<IAreaRepository> repository, Guid? tenantId)
        {
            var tenantContext = new Mock<ITenantContext>();
            tenantContext.Setup(t => t.CurrentTenantId).Returns(tenantId);

            return new GetAreaById(repository.Object, tenantContext.Object, TestMapper.Create());
        }

        [Fact]
        public async Task ExecuteAsync_Existing_ReturnsMappedResponse()
        {
            var repository = new Mock<IAreaRepository>();
            var tenantId = Guid.NewGuid();
            var entity = CreateArea(tenantId, "Ventas");
            repository
                .Setup(r => r.GetByIdAsync(tenantId, entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            var result = await BuildUseCase(repository, tenantId).ExecuteAsync(entity.Id);

            result.Id.Should().Be(entity.Id);
            result.TenantId.Should().Be(tenantId);
            result.Name.Should().Be("Ventas");
            result.IsActive.Should().BeTrue();
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
        }
    }
}
