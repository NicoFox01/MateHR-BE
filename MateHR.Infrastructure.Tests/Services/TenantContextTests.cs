using FluentAssertions;
using MateHR.Infrastructure.Services;
using Xunit;

namespace MateHR.Infrastructure.Tests.Services
{
    public class TenantContextTests
    {
        [Fact]
        public void CurrentTenantId_WhenNotSet_IsNull()
        {
            var context = new TenantContext();

            context.CurrentTenantId.Should().BeNull();
        }

        [Fact]
        public void SetCurrentTenantId_WithGuid_SetsValue()
        {
            var context = new TenantContext();
            var tenantId = Guid.NewGuid();

            context.SetCurrentTenantId(tenantId);

            context.CurrentTenantId.Should().Be(tenantId);
        }

        [Fact]
        public void SetCurrentTenantId_WithNull_ClearsValue()
        {
            var context = new TenantContext();
            context.SetCurrentTenantId(Guid.NewGuid());

            context.SetCurrentTenantId(null);

            context.CurrentTenantId.Should().BeNull();
        }
    }
}
