using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ASUDorms.Domain.Entities
{
    /// <summary>
    /// تصريحات - Payment Exemptions/Extensions
    /// Allows students to have meals even with outstanding payments
    /// </summary>
    public class PaymentExemption : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DormLocationId { get; set; }

        [Required]
        [MaxLength(20)]
        public string StudentId { get; set; }
        [Required]
        [MaxLength(25)]
        [ForeignKey(nameof(Student))]
        public string StudentNationalId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string ApprovedBy { get; set; } // اسم الموظف الذي وافق

        public DateTime ApprovedDate { get; set; }

        // Navigation Properties
        public virtual Student Student { get; set; }
    }
}