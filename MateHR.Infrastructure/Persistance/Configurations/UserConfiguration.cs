using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MateHR.Infrastructure.Persistance.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .ValueGeneratedNever();

            builder.Property(u => u.FirstName)
                .HasMaxLength(User.FirstNameMaxLength)
                .IsRequired();

            builder.Property(u => u.LastName)
                .HasMaxLength(User.LastNameMaxLength)
                .IsRequired();

            builder.Property(u => u.Email)
                .HasMaxLength(User.EmailMaxLength)
                .IsRequired();

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.PasswordHash)
                .HasMaxLength(User.PasswordHashMaxLength)
                .IsRequired();

            builder.Property(u => u.Role)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(u => u.SecurityStamp)
                .IsRequired();

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.HasIndex(u => u.TenantId);

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}