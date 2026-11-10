using MateHR.Domain.Recruitment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Infrastructure.Persistance.Configurations.Recruitment
{
    public class JobStatusConfiguration : IEntityTypeConfiguration<JobStatus>
    {
        public void Configure(EntityTypeBuilder<JobStatus> builder)
        {
            builder.ToTable("JobStatuses");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();
            builder.Property(s => s.Name).HasMaxLength(JobStatus.NameMaxLength).IsRequired();
            builder.HasIndex(s => new { s.TenantId, s.Name });

        }
    }
}
