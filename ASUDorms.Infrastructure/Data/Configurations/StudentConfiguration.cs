using ASUDorms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Data.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.HasOne(s => s.DormLocation)
                .WithMany(d => d.Students)
                .HasForeignKey(s => s.DormLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(s => s.Status)
                .HasConversion<string>();

            builder.Property(s => s.Religion)
                .HasConversion<string>();

            builder.Property(s => s.DormType)
                .HasConversion<string>();
        }
    }
}
