using FluentAssertions;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Infrastructure.Persistance;
using MateHR.Infrastructure.Persistance.Repositories.Recruitment;
using MateHR.Infrastructure.Tests.Common;

namespace MateHR.Infrastructure.Tests.Recruitment.Repositories
{
    public class AreaRepositoryTests
    {
        private static Area CreateArea(Guid tenantId, string name = "Nuevo Area") =>
            new(tenantId, name);

        private static async Task SeedAsync(ApplicationDbContext context, params Area[] areas)
        {
            context.Areas.AddRange(areas);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetByIdAsync_Existing_ReturnsEntity()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var expectedArea = CreateArea(tenantId, "en revision");
            await SeedAsync(context, expectedArea);
            context.ChangeTracker.Clear();

            var result = await repository.GetByIdAsync(tenantId, expectedArea.Id);

            result.Should().NotBeNull();
            result.Id.Should().Be(expectedArea.Id);
            result.TenantId.Should().Be(tenantId);
            result.Name.Should().Be("en revision");
        }

        [Fact]
        public async Task GetByIdAsync_Unknown_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var unknownId = Guid.NewGuid();

            Func<Task> act = () => repository.GetByIdAsync(tenantId, unknownId);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GetByIdAsync_OtherTenant_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var ownerTenantId = Guid.NewGuid();
            var otherTenantId = Guid.NewGuid();
            var seededArea = CreateArea(ownerTenantId, "en revision");
            await SeedAsync(context, seededArea);
            context.ChangeTracker.Clear();

            Func<Task> act = () => repository.GetByIdAsync(otherTenantId, seededArea.Id);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GetAllAsync_FirstPage_ReturnsTenantPageAndTotal()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();
            var tenantAAreas = Enumerable.Range(1, 25).Select(i => CreateArea(tenantA, $"Area {i:00}")).ToArray();
            var tenantBAreas = Enumerable.Range(1, 5).Select(i => CreateArea(tenantB, $"Area {i:00}")).ToArray();
            await SeedAsync(context, tenantAAreas.Concat(tenantBAreas).ToArray());
            context.ChangeTracker.Clear();

            var result = await repository.GetAllAsync(tenantA, pageNumber: 1, pageSize: 10);

            result.Items.Should().HaveCount(10);
            result.TotalCount.Should().Be(25);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.Items.Select(a => a.Name).Should().ContainInOrder(
                Enumerable.Range(1, 10).Select(i => $"Area {i:00}"));
            result.Items.Should().OnlyContain(a => a.TenantId == tenantA);
        }

        [Fact]
        public async Task GetAllAsync_ThirdPage_ReturnsRemainder()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();
            var tenantAAreas = Enumerable.Range(1, 25).Select(i => CreateArea(tenantA, $"Area {i:00}")).ToArray();
            var tenantBAreas = Enumerable.Range(1, 5).Select(i => CreateArea(tenantB, $"Area {i:00}")).ToArray();
            await SeedAsync(context, tenantAAreas.Concat(tenantBAreas).ToArray());
            context.ChangeTracker.Clear();

            var result = await repository.GetAllAsync(tenantA, pageNumber: 3, pageSize: 10);

            result.Items.Should().HaveCount(5);
            result.TotalCount.Should().Be(25);
            result.Items.Select(a => a.Name).Should().ContainInOrder(
                Enumerable.Range(21, 5).Select(i => $"Area {i:00}"));
        }

        [Fact]
        public async Task NameExistsAsync_Duplicate_ReturnsTrue()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            await SeedAsync(context, CreateArea(tenantId, "Ventas"));
            context.ChangeTracker.Clear();

            var result = await repository.NameExistsAsync(tenantId, "Ventas", excludeId: null);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task NameExistsAsync_ExcludingOwnId_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var seededArea = CreateArea(tenantId, "Ventas");
            await SeedAsync(context, seededArea);
            context.ChangeTracker.Clear();

            var result = await repository.NameExistsAsync(tenantId, "Ventas", excludeId: seededArea.Id);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task NameExistsAsync_DifferentTenant_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var ownerTenantId = Guid.NewGuid();
            var otherTenantId = Guid.NewGuid();
            await SeedAsync(context, CreateArea(ownerTenantId, "Ventas"));
            context.ChangeTracker.Clear();

            var result = await repository.NameExistsAsync(otherTenantId, "Ventas", excludeId: null);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task NameExistsAsync_DifferentCaseAndSpaces_ReturnsTrue()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            await SeedAsync(context, CreateArea(tenantId, "Ventas"));
            context.ChangeTracker.Clear();

            var result = await repository.NameExistsAsync(tenantId, "  ventas  ", excludeId: null);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task CreateAsync_Persists()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var newArea = CreateArea(tenantId, "Open");

            var result = await repository.CreateAsync(newArea);
            var reloaded = await repository.GetByIdAsync(tenantId, newArea.Id);

            result.Should().BeSameAs(newArea);
            reloaded.Id.Should().Be(newArea.Id);
            reloaded.TenantId.Should().Be(tenantId);
            reloaded.Name.Should().Be("Open");
        }

        [Fact]
        public async Task RenameAsync_ChangesName()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var seededArea = CreateArea(tenantId, "Ventas");
            await SeedAsync(context, seededArea);
            context.ChangeTracker.Clear();

            await repository.RenameAsync(tenantId, seededArea.Id, "  Nuevo  ");
            var reloaded = await repository.GetByIdAsync(tenantId, seededArea.Id);

            reloaded.Name.Should().Be("Nuevo");
        }

        [Fact]
        public async Task RenameAsync_UnknownOrOtherTenant_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var ownerTenantId = Guid.NewGuid();
            var otherTenantId = Guid.NewGuid();
            var seededArea = CreateArea(ownerTenantId, "Ventas");
            await SeedAsync(context, seededArea);
            context.ChangeTracker.Clear();

            Func<Task> unknownIdAct = () => repository.RenameAsync(ownerTenantId, Guid.NewGuid(), "Nuevo");
            Func<Task> otherTenantAct = () => repository.RenameAsync(otherTenantId, seededArea.Id, "Nuevo");

            await unknownIdAct.Should().ThrowAsync<KeyNotFoundException>();
            await otherTenantAct.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_Removes()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var seededArea = CreateArea(tenantId, "Ventas");
            await SeedAsync(context, seededArea);
            context.ChangeTracker.Clear();

            await repository.DeleteAsync(tenantId, seededArea.Id);
            Func<Task> act = () => repository.GetByIdAsync(tenantId, seededArea.Id);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_UnknownOrOtherTenant_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var ownerTenantId = Guid.NewGuid();
            var otherTenantId = Guid.NewGuid();
            var seededArea = CreateArea(ownerTenantId, "Ventas");
            await SeedAsync(context, seededArea);
            context.ChangeTracker.Clear();

            Func<Task> unknownIdAct = () => repository.DeleteAsync(ownerTenantId, Guid.NewGuid());
            Func<Task> otherTenantAct = () => repository.DeleteAsync(otherTenantId, seededArea.Id);

            await unknownIdAct.Should().ThrowAsync<KeyNotFoundException>();
            await otherTenantAct.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task IsInUseAsync_NoJobsYet_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var seededArea = CreateArea(tenantId, "en revision");
            await SeedAsync(context, seededArea);
            context.ChangeTracker.Clear();

            var result = await repository.IsInUseAsync(tenantId, seededArea.Id);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_Default_ExcludesInactive()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var activeOne = CreateArea(tenantId, "Activa Uno");
            var activeTwo = CreateArea(tenantId, "Activa Dos");
            var inactive = CreateArea(tenantId, "Inactiva");
            inactive.Deactivate();
            await SeedAsync(context, activeOne, activeTwo, inactive);
            context.ChangeTracker.Clear();

            var result = await repository.GetAllAsync(tenantId, pageNumber: 1, pageSize: 10);

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(a => a.IsActive);
            result.Items.Select(a => a.Name).Should().NotContain("Inactiva");
        }

        [Fact]
        public async Task GetAllAsync_IncludeInactive_ReturnsBoth()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var active = CreateArea(tenantId, "Activa");
            var inactive = CreateArea(tenantId, "Inactiva");
            inactive.Deactivate();
            await SeedAsync(context, active, inactive);
            context.ChangeTracker.Clear();

            var result = await repository.GetAllAsync(tenantId, pageNumber: 1, pageSize: 10, includeInactive: true);

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Select(a => a.Name).Should().Contain("Activa").And.Contain("Inactiva");
        }

        [Fact]
        public async Task SetActiveAsync_Deactivates()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var seededArea = CreateArea(tenantId, "Ventas");
            await SeedAsync(context, seededArea);
            context.ChangeTracker.Clear();

            await repository.SetActiveAsync(tenantId, seededArea.Id, isActive: false);
            var reloaded = await repository.GetByIdAsync(tenantId, seededArea.Id);

            reloaded.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task SetActiveAsync_Activates()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var seededArea = CreateArea(tenantId, "Ventas");
            seededArea.Deactivate();
            await SeedAsync(context, seededArea);
            context.ChangeTracker.Clear();

            await repository.SetActiveAsync(tenantId, seededArea.Id, isActive: true);
            var reloaded = await repository.GetByIdAsync(tenantId, seededArea.Id);

            reloaded.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task SetActiveAsync_UnknownOrOtherTenant_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var ownerTenantId = Guid.NewGuid();
            var otherTenantId = Guid.NewGuid();
            var seededArea = CreateArea(ownerTenantId, "Ventas");
            await SeedAsync(context, seededArea);
            context.ChangeTracker.Clear();

            Func<Task> unknownIdAct = () => repository.SetActiveAsync(ownerTenantId, Guid.NewGuid(), isActive: false);
            Func<Task> otherTenantAct = () => repository.SetActiveAsync(otherTenantId, seededArea.Id, isActive: false);

            await unknownIdAct.Should().ThrowAsync<KeyNotFoundException>();
            await otherTenantAct.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task NameExistsAsync_InactiveStillCounts_ReturnsTrue()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new AreaRepository(context);
            var tenantId = Guid.NewGuid();
            var inactive = CreateArea(tenantId, "Ventas");
            inactive.Deactivate();
            await SeedAsync(context, inactive);
            context.ChangeTracker.Clear();

            var result = await repository.NameExistsAsync(tenantId, "Ventas", excludeId: null);

            result.Should().BeTrue();
        }
    }
}
