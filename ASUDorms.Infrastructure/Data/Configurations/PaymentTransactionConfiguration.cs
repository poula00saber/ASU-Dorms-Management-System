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
    public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.HasOne(pt => pt.Student)
                .WithMany(s => s.PaymentTransactions)
                .HasForeignKey(pt => pt.StudentNationalId)
                .OnDelete(DeleteBehavior.Cascade);



            builder.Property(pt => pt.PaymentType)
                .HasConversion<string>();

            builder.Property(pt => pt.Amount)
                .HasPrecision(18, 2);

            builder.HasIndex(pt => pt.StudentNationalId)
                .HasDatabaseName("IX_PaymentTransactions_Student");

            builder.HasIndex(pt => pt.PaymentDate)
                .HasDatabaseName("IX_PaymentTransactions_Date");
        }
    }
}
