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
        }
    }
}
