using FluentAssertions;
using MateHR.Domain.Recruitment.Entities;
using MateHR.Infrastructure.Persistance;
using MateHR.Infrastructure.Persistance.Repositories.Recruitment;
using MateHR.Infrastructure.Tests.Common;

namespace MateHR.Infrastructure.Tests.Recruitment.Repositories
{
    public class JobStatusRepositoryTests
    {
        private static JobStatus CreateJobStatus(Guid tenantId, string name = "Nueva Area") =>
            new(tenantId, name);

        private static async Task SeedAsync(ApplicationDbContext context, params JobStatus[] jobStatuses)
        {
            context.JobStatuses.AddRange(jobStatuses);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetByIdAsync_Existing_ReturnsEntity()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var expectedStatus = CreateJobStatus(tenantId, "en revision");
            await SeedAsync(context, expectedStatus);
            context.ChangeTracker.Clear();

            var result = await repository.GetByIdAsync(tenantId, expectedStatus.Id);

            result.Should().NotBeNull();
            result.Id.Should().Be(expectedStatus.Id);
            result.TenantId.Should().Be(tenantId);
            result.Name.Should().Be("en revision");
        }

        [Fact]
        public async Task GetByIdAsync_Unknown_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var unknownId = Guid.NewGuid();

            Func<Task> act = () => repository.GetByIdAsync(tenantId, unknownId);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GetByIdAsync_OtherTenant_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var ownerTenantId = Guid.NewGuid();
            var otherTenantId = Guid.NewGuid();
            var seededStatus = CreateJobStatus(ownerTenantId, "en revision");
            await SeedAsync(context, seededStatus);
            context.ChangeTracker.Clear();

            Func<Task> act = () => repository.GetByIdAsync(otherTenantId, seededStatus.Id);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GetAllAsync_PagesAndCounts()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var otherTenantId = Guid.NewGuid();
            await SeedAsync(
                context,
                CreateJobStatus(tenantId, "Z"),
                CreateJobStatus(tenantId, "A"),
                CreateJobStatus(tenantId, "M"),
                CreateJobStatus(tenantId, "B"),
                CreateJobStatus(tenantId, "C"),
                CreateJobStatus(otherTenantId, "X"));
            context.ChangeTracker.Clear();

            var result = await repository.GetAllAsync(tenantId, pageNumber: 2, pageSize: 2);

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(5);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(2);
            result.Items.Select(js => js.Name).Should().ContainInOrder("C", "M");
            result.Items.Should().OnlyContain(js => js.TenantId == tenantId);
        }

        [Fact]
        public async Task NameExistsAsync_Duplicate_ReturnsTrue()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            await SeedAsync(context, CreateJobStatus(tenantId, "Active"));
            context.ChangeTracker.Clear();

            var result = await repository.NameExistsAsync(tenantId, "Active", excludeId: null);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task NameExistsAsync_DifferentCaseAndSpaces_ReturnsTrue()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            await SeedAsync(context, CreateJobStatus(tenantId, "Abierto"));
            context.ChangeTracker.Clear();

            var result = await repository.NameExistsAsync(tenantId, "  abierto  ", excludeId: null);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task NameExistsAsync_ExcludingOwnId_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var seededStatus = CreateJobStatus(tenantId, "Active");
            await SeedAsync(context, seededStatus);
            context.ChangeTracker.Clear();

            var result = await repository.NameExistsAsync(tenantId, "Active", excludeId: seededStatus.Id);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task NameExistsAsync_DifferentTenant_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var ownerTenantId = Guid.NewGuid();
            var otherTenantId = Guid.NewGuid();
            await SeedAsync(context, CreateJobStatus(ownerTenantId, "Active"));
            context.ChangeTracker.Clear();

            var result = await repository.NameExistsAsync(otherTenantId, "Active", excludeId: null);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_Persists()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var newStatus = CreateJobStatus(tenantId, "Open");

            var result = await repository.CreateAsync(newStatus);
            var reloaded = await repository.GetByIdAsync(tenantId, newStatus.Id);

            result.Should().BeSameAs(newStatus);
            reloaded.Id.Should().Be(newStatus.Id);
            reloaded.TenantId.Should().Be(tenantId);
            reloaded.Name.Should().Be("Open");
        }

        [Fact]
        public async Task RenameAsync_ChangesName()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var seededStatus = CreateJobStatus(tenantId, "Active");
            await SeedAsync(context, seededStatus);
            context.ChangeTracker.Clear();

            await repository.RenameAsync(tenantId, seededStatus.Id, "  Paused  ");
            var reloaded = await repository.GetByIdAsync(tenantId, seededStatus.Id);

            reloaded.Name.Should().Be("Paused");
        }

        [Fact]
        public async Task RenameAsync_UnknownOrOtherTenant_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var ownerTenantId = Guid.NewGuid();
            var otherTenantId = Guid.NewGuid();
            var seededStatus = CreateJobStatus(ownerTenantId, "Active");
            await SeedAsync(context, seededStatus);
            context.ChangeTracker.Clear();

            Func<Task> unknownIdAct = () => repository.RenameAsync(ownerTenantId, Guid.NewGuid(), "Paused");
            Func<Task> otherTenantAct = () => repository.RenameAsync(otherTenantId, seededStatus.Id, "Paused");

            await unknownIdAct.Should().ThrowAsync<KeyNotFoundException>();
            await otherTenantAct.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_Removes()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var seededStatus = CreateJobStatus(tenantId, "Active");
            await SeedAsync(context, seededStatus);
            context.ChangeTracker.Clear();

            await repository.DeleteAsync(tenantId, seededStatus.Id);
            Func<Task> act = () => repository.GetByIdAsync(tenantId, seededStatus.Id);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task DeleteAsync_UnknownOrOtherTenant_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var ownerTenantId = Guid.NewGuid();
            var otherTenantId = Guid.NewGuid();
            var seededStatus = CreateJobStatus(ownerTenantId, "Active");
            await SeedAsync(context, seededStatus);
            context.ChangeTracker.Clear();

            Func<Task> unknownIdAct = () => repository.DeleteAsync(ownerTenantId, Guid.NewGuid());
            Func<Task> otherTenantAct = () => repository.DeleteAsync(otherTenantId, seededStatus.Id);

            await unknownIdAct.Should().ThrowAsync<KeyNotFoundException>();
            await otherTenantAct.Should().ThrowAsync<KeyNotFoundException>();
        }

        // 13) IsInUseAsync_NoJobsYet_ReturnsFalse (D-01: la entidad Job no existe hasta REC-08)
        // Dado: sembrás un JobStatus
        // Acción: IsInUseAsync(tenantId, id)
        // Esperado: false, todavia no hay nada que lo referencie

        [Fact]
        public async Task IsInUseAsync_NoJobsYet_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var seededStatus = CreateJobStatus(tenantId, "en revision");
            await SeedAsync(context, seededStatus);
            context.ChangeTracker.Clear();

            var result = await repository.IsInUseAsync(tenantId, seededStatus.Id);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllAsync_Default_ExcludesInactive()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var activeOne = CreateJobStatus(tenantId, "Activo Uno");
            var activeTwo = CreateJobStatus(tenantId, "Activo Dos");
            var inactive = CreateJobStatus(tenantId, "Inactivo");
            inactive.Deactivate();
            await SeedAsync(context, activeOne, activeTwo, inactive);
            context.ChangeTracker.Clear();

            var result = await repository.GetAllAsync(tenantId, pageNumber: 1, pageSize: 10);

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(js => js.IsActive);
            result.Items.Select(js => js.Name).Should().NotContain("Inactivo");
        }

        [Fact]
        public async Task GetAllAsync_IncludeInactive_ReturnsBoth()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var active = CreateJobStatus(tenantId, "Activo");
            var inactive = CreateJobStatus(tenantId, "Inactivo");
            inactive.Deactivate();
            await SeedAsync(context, active, inactive);
            context.ChangeTracker.Clear();

            var result = await repository.GetAllAsync(tenantId, pageNumber: 1, pageSize: 10, includeInactive: true);

            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Select(js => js.Name).Should().Contain("Activo").And.Contain("Inactivo");
        }

        [Fact]
        public async Task SetActiveAsync_Deactivates()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var seededStatus = CreateJobStatus(tenantId, "Active");
            await SeedAsync(context, seededStatus);
            context.ChangeTracker.Clear();

            await repository.SetActiveAsync(tenantId, seededStatus.Id, isActive: false);
            var reloaded = await repository.GetByIdAsync(tenantId, seededStatus.Id);

            reloaded.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task SetActiveAsync_Activates()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var seededStatus = CreateJobStatus(tenantId, "Active");
            seededStatus.Deactivate();
            await SeedAsync(context, seededStatus);
            context.ChangeTracker.Clear();

            await repository.SetActiveAsync(tenantId, seededStatus.Id, isActive: true);
            var reloaded = await repository.GetByIdAsync(tenantId, seededStatus.Id);

            reloaded.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task SetActiveAsync_UnknownOrOtherTenant_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var ownerTenantId = Guid.NewGuid();
            var otherTenantId = Guid.NewGuid();
            var seededStatus = CreateJobStatus(ownerTenantId, "Active");
            await SeedAsync(context, seededStatus);
            context.ChangeTracker.Clear();

            Func<Task> unknownIdAct = () => repository.SetActiveAsync(ownerTenantId, Guid.NewGuid(), isActive: false);
            Func<Task> otherTenantAct = () => repository.SetActiveAsync(otherTenantId, seededStatus.Id, isActive: false);

            await unknownIdAct.Should().ThrowAsync<KeyNotFoundException>();
            await otherTenantAct.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task NameExistsAsync_InactiveStillCounts_ReturnsTrue()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new JobStatusRepository(context);
            var tenantId = Guid.NewGuid();
            var inactive = CreateJobStatus(tenantId, "Active");
            inactive.Deactivate();
            await SeedAsync(context, inactive);
            context.ChangeTracker.Clear();

            var result = await repository.NameExistsAsync(tenantId, "Active", excludeId: null);

            result.Should().BeTrue();
        }
    }
}
