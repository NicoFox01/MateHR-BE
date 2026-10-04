using MateHR.Domain.Tenants.Entities;
using Microsoft.EntityFrameworkCore;

namespace MateHR.Infrastructure.Persistance
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            StampUpdatedAt();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            StampUpdatedAt();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void StampUpdatedAt()
        {
            var now = DateTimeOffset.UtcNow;

            foreach (var entry in ChangeTracker.Entries<Tenant>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(Tenant.UpdatedAt)).CurrentValue = now;
                }
            }
        }
    }
}