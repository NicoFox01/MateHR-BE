using FluentAssertions;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.Enums;
using MateHR.Infrastructure.Persistance;
using MateHR.Infrastructure.Persistance.Repositories;
using MateHR.Infrastructure.Tests.Common;
using Xunit;

namespace MateHR.Infrastructure.Tests.Persistance.Repositories
{
    public class TenantRepositoryTests
    {
        private static Tenant CreateTenant(string name, string slug, TenantStatus status = TenantStatus.Active) => new(
            name,
            slug,
            "20304050601",
            "owner@acme.com",
            Industry.Technology,
            null,
            status,
            RecruitmentMode.Internal,
            SubscriptionType.Free);

        private static async Task SeedAsync(ApplicationDbContext context, params Tenant[] tenants)
        {
            context.Tenants.AddRange(tenants);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetTenantByIdAsync_ExistingTenant_ReturnsTenant()
        {
            using var context = InMemoryDbContextFactory.Create();
            var tenant = CreateTenant("Acme Corp", "acme");
            await SeedAsync(context, tenant);
            var repository = new TenantRepository(context);

            var result = await repository.GetTenantByIdAsync(tenant.Id);

            result.Id.Should().Be(tenant.Id);
            result.Name.Should().Be("Acme Corp");
            result.Slug.Should().Be("acme");
            result.CUIT.Should().Be("20304050601");
            result.OwnerEmail.Should().Be("owner@acme.com");
            result.Status.Should().Be(TenantStatus.Active);
        }

        [Fact]
        public async Task GetTenantByIdAsync_UnknownId_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new TenantRepository(context);
            var unknownId = Guid.NewGuid();

            Func<Task> act = () => repository.GetTenantByIdAsync(unknownId);

            var exception = await act.Should().ThrowAsync<KeyNotFoundException>();
            exception.Which.Message.Should().Contain(unknownId.ToString());
        }

        [Fact]
        public async Task GetTenantBySlugAsync_ExistingSlug_ReturnsTenant()
        {
            using var context = InMemoryDbContextFactory.Create();
            var tenant = CreateTenant("Acme Corp", "acme");
            await SeedAsync(context, tenant);
            var repository = new TenantRepository(context);

            var result = await repository.GetTenantBySlugAsync("acme");

            result.Id.Should().Be(tenant.Id);
        }

        [Fact]
        public async Task GetTenantBySlugAsync_UnknownSlug_ThrowsKeyNotFoundException()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new TenantRepository(context);

            Func<Task> act = () => repository.GetTenantBySlugAsync("no-existe");

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GetTenantsAsync_Page2Size2_ReturnsPageAndTotalCount()
        {
            using var context = InMemoryDbContextFactory.Create();
            await SeedAsync(
                context,
                CreateTenant("Alpha", "alpha"),
                CreateTenant("Bravo", "bravo"),
                CreateTenant("Charlie", "charlie"),
                CreateTenant("Delta", "delta"),
                CreateTenant("Echo", "echo"));
            var repository = new TenantRepository(context);

            var result = await repository.GetTenantsAsync(null, 2, 2);

            result.Items.Should().HaveCount(2);
            result.Items.Select(t => t.Name).Should().ContainInOrder("Charlie", "Delta");
            result.TotalCount.Should().Be(5);
            result.PageNumber.Should().Be(2);
            result.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task GetTenantsAsync_WithStatusFilter_ReturnsOnlyMatching()
        {
            using var context = InMemoryDbContextFactory.Create();
            await SeedAsync(
                context,
                CreateTenant("Active One", "active-one", TenantStatus.Active),
                CreateTenant("Active Two", "active-two", TenantStatus.Active),
                CreateTenant("Active Three", "active-three", TenantStatus.Active),
                CreateTenant("Suspended One", "suspended-one", TenantStatus.Suspended),
                CreateTenant("Suspended Two", "suspended-two", TenantStatus.Suspended));
            var repository = new TenantRepository(context);

            var result = await repository.GetTenantsAsync(TenantStatus.Active, 1, 10);

            result.Items.Should().HaveCount(3);
            result.Items.Should().OnlyContain(t => t.Status == TenantStatus.Active);
            result.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetTenantsAsync_ReturnsOrderedByName()
        {
            using var context = InMemoryDbContextFactory.Create();
            await SeedAsync(
                context,
                CreateTenant("Zeta", "zeta"),
                CreateTenant("Alpha", "alpha"),
                CreateTenant("Mid", "mid"));
            var repository = new TenantRepository(context);

            var result = await repository.GetTenantsAsync(null, 1, 10);

            result.Items.Select(t => t.Name).Should().ContainInOrder("Alpha", "Mid", "Zeta");
        }

        [Fact]
        public async Task ExistsBySlugAsync_ExistingSlug_ReturnsTrue()
        {
            using var context = InMemoryDbContextFactory.Create();
            await SeedAsync(context, CreateTenant("Acme Corp", "acme"));
            var repository = new TenantRepository(context);

            var result = await repository.ExistsBySlugAsync("acme", null);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsBySlugAsync_UnknownSlug_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new TenantRepository(context);

            var result = await repository.ExistsBySlugAsync("no-existe", null);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsBySlugAsync_ExcludingOwnTenant_ReturnsFalse()
        {
            using var context = InMemoryDbContextFactory.Create();
            var tenant = CreateTenant("Acme Corp", "acme");
            await SeedAsync(context, tenant);
            var repository = new TenantRepository(context);

            var result = await repository.ExistsBySlugAsync("acme", tenant.Id);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CreateTenantAsync_PersistsTenant()
        {
            using var context = InMemoryDbContextFactory.Create();
            var repository = new TenantRepository(context);
            var tenant = CreateTenant("Acme Corp", "acme");

            var created = await repository.CreateTenantAsync(tenant);

            created.Id.Should().Be(tenant.Id);
            var reloaded = await repository.GetTenantByIdAsync(tenant.Id);
            reloaded.Name.Should().Be("Acme Corp");
            reloaded.Slug.Should().Be("acme");
            reloaded.CUIT.Should().Be("20304050601");
            reloaded.OwnerEmail.Should().Be("owner@acme.com");
        }

        [Fact]
        public async Task UpdateStatusTenantAsync_ChangesStatusAndSetsUpdatedAt()
        {
            using var context = InMemoryDbContextFactory.Create();
            var tenant = CreateTenant("Acme Corp", "acme");
            await SeedAsync(context, tenant);
            var repository = new TenantRepository(context);
            await repository.GetTenantByIdAsync(tenant.Id);

            var result = await repository.UpdateStatusTenantAsync(tenant.Id, TenantStatus.Suspended);

            result.Status.Should().Be(TenantStatus.Suspended);
            result.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateRecruitmentModeTenantAsync_ChangesMode()
        {
            using var context = InMemoryDbContextFactory.Create();
            var tenant = CreateTenant("Acme Corp", "acme");
            await SeedAsync(context, tenant);
            var repository = new TenantRepository(context);
            await repository.GetTenantByIdAsync(tenant.Id);

            await repository.UpdateRecruitmentModeTenantAsync(tenant.Id, RecruitmentMode.External);

            var reloaded = await repository.GetTenantByIdAsync(tenant.Id);
            reloaded.RecruitmentMode.Should().Be(RecruitmentMode.External);
        }

        [Fact]
        public async Task UpdateSubscriptionTypeTenantAsync_ChangesType()
        {
            using var context = InMemoryDbContextFactory.Create();
            var tenant = CreateTenant("Acme Corp", "acme");
            await SeedAsync(context, tenant);
            var repository = new TenantRepository(context);
            await repository.GetTenantByIdAsync(tenant.Id);

            await repository.UpdateSubscriptionTypeTenantAsync(tenant.Id, SubscriptionType.Yearly);

            var reloaded = await repository.GetTenantByIdAsync(tenant.Id);
            reloaded.SubscriptionType.Should().Be(SubscriptionType.Yearly);
            reloaded.SubscriptionStatus.Should().Be(SubscriptionStatus.None);
        }
    }
}
