using MateHR.Domain.Recruitment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Infrastructure.Persistance.Configurations.Recruitment
{
    public class AreaConfiguration : IEntityTypeConfiguration<Area>
    {
        public void Configure(EntityTypeBuilder<Area> builder)
        {
            builder.ToTable("Areas");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();
            builder.Property(s => s.Name).HasMaxLength(Area.NameMaxLength).IsRequired();
            builder.Property(s => s.IsActive).IsRequired();
            builder.HasIndex(s => new { s.TenantId, s.Name });
        }
    }
}
