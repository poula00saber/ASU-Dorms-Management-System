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
            // Composite Primary Key
            builder.HasKey(s => s.NationalId);

            // Relationships
            builder.HasOne(s => s.DormLocation)
                .WithMany(d => d.Students)
                .HasForeignKey(s => s.DormLocationId)
                .OnDelete(DeleteBehavior.Restrict);


            // Enum conversions
            builder.Property(s => s.Status)
                .HasConversion<string>();

            builder.Property(s => s.Religion)
                .HasConversion<string>();

            builder.Property(s => s.DormType)
                .HasConversion<string>();

            // Indexes for better query performance
            builder.HasIndex(s => s.NationalId)
                .HasDatabaseName("IX_Students_NationalId");

            builder.HasIndex(s => new { s.DormLocationId, s.BuildingNumber })
                .HasDatabaseName("IX_Students_DormLocation_Building");

            builder.HasIndex(s => new { s.DormLocationId, s.Faculty })
                .HasDatabaseName("IX_Students_DormLocation_Faculty");

            // Decimal precision
            builder.Property(s => s.PercentageGrade)
                .HasPrecision(5, 2);

            builder.Property(s => s.HighSchoolPercentage)
                .HasPrecision(5, 2);

            builder.Property(s => s.OutstandingAmount)
                .HasPrecision(18, 2);



            // Critical for scanner performance (search by NationalId)
            builder.HasIndex(s => s.NationalId)
                .HasDatabaseName("IX_Students_NationalId")
                .IsUnique(); // Since it's the PK, but explicit index helps

            // For searching by student ID
            builder.HasIndex(s => s.StudentId)
                .HasDatabaseName("IX_Students_StudentId");

            // For filtering by dorm location (used in global query filter)
            builder.HasIndex(s => s.DormLocationId)
                .HasDatabaseName("IX_Students_DormLocationId");

            // For common queries
            builder.HasIndex(s => new { s.StudentId, s.DormLocationId })
                .HasDatabaseName("IX_Students_StudentId_DormLocationId")
                .IsUnique();






        }
    }
}
