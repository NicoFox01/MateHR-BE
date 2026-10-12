using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MateHR.Infrastructure.Persistance.Configurations
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenants");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .ValueGeneratedNever();

            builder.Property(t => t.Name)
                .HasMaxLength(Tenant.NameMaxLength)
                .IsRequired();

            builder.Property(t => t.Slug)
                .HasMaxLength(Tenant.SlugMaxLength)
                .IsRequired();

            builder.HasIndex(t => t.Slug)
                .IsUnique();

            builder.Property(t => t.CUIT)
                .HasMaxLength(Tenant.CuitMaxLength)
                .IsRequired();

            builder.Property(t => t.OwnerEmail)
                .HasMaxLength(Tenant.OwnerEmailMaxLength)
                .IsRequired();

            builder.Property(t => t.CreatedAt)
                .IsRequired();

            builder.Property(t => t.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.Industry)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.RecruitmentMode)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.SubscriptionType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.SubscriptionStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(t => t.SubscriptionExpiresAt);

            builder.OwnsOne(t => t.Address, address =>
            {
                address.Property(a => a.Street).HasMaxLength(Address.StreetMaxLength);
                address.Property(a => a.City).HasMaxLength(Address.CityMaxLength);
                address.Property(a => a.State).HasMaxLength(Address.StateMaxLength);
                address.Property(a => a.Country).HasMaxLength(Address.CountryMaxLength);
                address.Property(a => a.PostalCode).HasMaxLength(Address.PostalCodeMaxLength);
            });

            builder.Navigation(t => t.Address)
                .IsRequired(false);
        }
    }
}