using FluentAssertions;
using MateHR.Domain.Recruitment.Entities;


namespace MateHR.Domain.Tests.Recruitment.Entities
{
    public class AreaTests
    {
        [Fact]
        public void Constructor_ValidData_SetsDefaults()
        {
            var tenantId = Guid.NewGuid();
            var area = new Area(tenantId, "Ventas");
            area.Id.Should().NotBe(Guid.Empty);
            area.TenantId.Should().Be(tenantId);
            area.Name.Should().Be("Ventas");
            area.IsActive.Should().BeTrue();
        }
        [Fact]
        public void Constructor_EmptyTenant_ThrowsArgumentException()
        {
            Action act = () => new Area(Guid.Empty, "Ventas");
            act.Should().Throw<ArgumentException>()
                .Which.ParamName.Should().Be("tenantId");
        }
        [Fact]
        public void Constructor_WhitespaceName_ThrowsArgumentException()
        {
            Action act = () => new Area(Guid.NewGuid(), "   ");
            act.Should().Throw<ArgumentException>()
                .Which.ParamName.Should().Be("name");
        }
        [Fact]
        public void Constructor_TrimsName()
        {
            var area = new Area(Guid.NewGuid(), "  Ventas  ");
            area.Name.Should().Be("Ventas");
        }
        [Fact]
        public void Constructor_NameTooLong_ThrowsArgumentException()
        {
            var tooLongName = new string('a', Area.NameMaxLength + 1);
            Action act = () => new Area(Guid.NewGuid(), tooLongName);
            act.Should().Throw<ArgumentException>()
                .Which.ParamName.Should().Be("name");
        }
        [Fact]
        public void Rename_ValidName_ChangesName()
        {
            var area = new Area(Guid.NewGuid(), "Ventas");
            area.Rename("  Marketing    ");
            area.Name.Should().Be("Marketing");
        }
        [Fact]
        public void Rename_EmptyName_ThrowsAndKeepsName()
        {
            var area = new Area(Guid.NewGuid(), "Ventas");
            Action act = () => area.Rename("");
            act.Should().Throw<ArgumentException>()
                .Which.ParamName.Should().Be("name");
            area.Name.Should().Be("Ventas");
        }
        [Fact]
        public void Deactivate_SetsInactive()
        {
            var area = new Area(Guid.NewGuid(), "Ventas");
            area.Deactivate();
            area.IsActive.Should().BeFalse();
        }
        [Fact]
        public void Activate_SetsActive()
        { 
            var area = new Area(Guid.NewGuid(), "Ventas");
            area.Deactivate();
            area.Activate();
            area.IsActive.Should().BeTrue();
        }
        [Fact]
        public void Deactivate_AlreadyInactive_StaysInactive()
        {
            var area = new Area(Guid.NewGuid(), "Ventas");
            area.Deactivate();
            area.Deactivate();
            area.IsActive.Should().BeFalse();
        }
        [Fact]
        public void Activate_AlreadyActive_StaysActive()
        {
            var area = new Area(Guid.NewGuid(), "Ventas");
            area.Activate();
            area.Activate();
            area.IsActive.Should().BeTrue();
        }
    }
}
