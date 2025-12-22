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
    public class MealTransactionConfiguration : IEntityTypeConfiguration<MealTransaction>
    {
        public void Configure(EntityTypeBuilder<MealTransaction> builder)
        {
            builder.HasOne(m => m.Student)
                .WithMany(s => s.MealTransactions)
                .HasForeignKey(m => m.StudentNationalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.MealType)
                .WithMany(mt => mt.MealTransactions)
                .HasForeignKey(m => m.MealTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.DormLocation)
                .WithMany(d => d.MealTransactions)
                .HasForeignKey(m => m.DormLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.ScannedByUser)
                .WithMany()
                .HasForeignKey(m => m.ScannedByUserId)
                .OnDelete(DeleteBehavior.Restrict);



            // Critical for scanner duplicate check (MOST IMPORTANT!)
            builder.HasIndex(m => new { m.StudentNationalId, m.Date, m.MealTypeId })
                .HasDatabaseName("IX_MealTransactions_Student_Date_MealType")
                .IsUnique(false); // Not unique since multiple scans per day

            // For filtering by dorm location (used in global query filter)
            builder.HasIndex(m => m.DormLocationId)
                .HasDatabaseName("IX_MealTransactions_DormLocationId");

            // For searching by student
            builder.HasIndex(m => m.StudentNationalId)
                .HasDatabaseName("IX_MealTransactions_StudentNationalId");

            // For date-based queries
            builder.HasIndex(m => m.Date)
                .HasDatabaseName("IX_MealTransactions_Date");

            // For time-based queries
            builder.HasIndex(m => m.Time)
                .HasDatabaseName("IX_MealTransactions_Time");

            // For scanner user tracking
            builder.HasIndex(m => m.ScannedByUserId)
                .HasDatabaseName("IX_MealTransactions_ScannedByUserId");


        }
    }
}
