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
    public class PaymentExemptionConfiguration : IEntityTypeConfiguration<PaymentExemption>
    {
        public void Configure(EntityTypeBuilder<PaymentExemption> builder)
        {
            builder.HasOne(pe => pe.Student)
                .WithMany(s => s.PaymentExemptions)
                .HasForeignKey(pe => pe.StudentNationalId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(pe => new { pe.StudentNationalId, pe.IsActive })
                .HasDatabaseName("IX_PaymentExemptions_Student_Active");
        }
    }

}
