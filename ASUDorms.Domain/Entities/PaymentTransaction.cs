using ASUDorms.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Domain.Entities
{
    public class PaymentTransaction : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DormLocationId { get; set; }

        [Required]
        [MaxLength(25)]
        [ForeignKey(nameof(Student))]
        public string StudentNationalId { get; set; }
        [Required]
        [MaxLength(20)]
        public string StudentId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentType PaymentType { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [MaxLength(100)]
        public string ReceiptNumber { get; set; } // رقم الإيصال

        // For monthly payments
        public int? Month { get; set; }
        public int? Year { get; set; }

        // For missed meal payments
        public int? MissedMealsCount { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        public string ProcessedBy { get; set; } // اسم الموظف

        public virtual Student Student { get; set; }
    }
}
