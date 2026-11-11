using FluentAssertions;
using MateHR.Domain.Recruitment.Entities;

namespace MateHR.Domain.Tests.Recruitment.Entities
{
    public class JobStatusTests
    {
        [Fact]
        public void Constructor_ValidData_SetsDefaults()
        {
            var tenantId = Guid.NewGuid();

            var jobStatus = new JobStatus(tenantId, "Active");

            jobStatus.Id.Should().NotBe(Guid.Empty);
            jobStatus.TenantId.Should().Be(tenantId);
            jobStatus.Name.Should().Be("Active");
        }

        [Fact]
        public void Constructor_EmptyTenant_ThrowsArgumentException()
        {
            Action act = () => new JobStatus(Guid.Empty, "Active");

            act.Should().Throw<ArgumentException>()
                .Which.ParamName.Should().Be("tenantId");
        }

        [Fact]
        public void Constructor_WhitespaceName_ThrowsArgumentException()
        {
            Action act = () => new JobStatus(Guid.NewGuid(), "   ");

            act.Should().Throw<ArgumentException>()
                .Which.ParamName.Should().Be("name");
        }

        [Fact]
        public void Constructor_NameTooLong_ThrowsArgumentException()
        {
            var tooLongName = new string('a', JobStatus.NameMaxLength + 1);

            Action act = () => new JobStatus(Guid.NewGuid(), tooLongName);

            act.Should().Throw<ArgumentException>()
                .Which.ParamName.Should().Be("name");
        }

        [Fact]
        public void Constructor_TrimsName()
        {
            var jobStatus = new JobStatus(Guid.NewGuid(), "  Active  ");

            jobStatus.Name.Should().Be("Active");
        }

        [Fact]
        public void Rename_ValidName_ChangesName()
        {
            var jobStatus = new JobStatus(Guid.NewGuid(), "Active");

            jobStatus.Rename("  Paused  ");

            jobStatus.Name.Should().Be("Paused");
        }

        [Fact]
        public void Rename_Whitespace_ThrowsAndKeepsName()
        {
            var jobStatus = new JobStatus(Guid.NewGuid(), "Active");

            Action act = () => jobStatus.Rename("   ");

            act.Should().Throw<ArgumentException>()
                .Which.ParamName.Should().Be("name");
            jobStatus.Name.Should().Be("Active");
        }
    }
}