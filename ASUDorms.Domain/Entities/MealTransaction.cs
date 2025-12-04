using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Domain.Entities
{
    public class MealTransaction : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(25)]
        [ForeignKey(nameof(Student))]
        public string StudentNationalId { get; set; }   // FK → Student
        [Required]
        [MaxLength(20)]
        public string StudentId { get; set; }

        [Required]
        [ForeignKey(nameof(MealType))]
        public int MealTypeId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan Time { get; set; }

        [Required]
        [ForeignKey(nameof(DormLocation))]
        public int DormLocationId { get; set; }

        [Required]
        [ForeignKey(nameof(ScannedByUser))]
        public int ScannedByUserId { get; set; }

        // Navigation Properties
        public virtual Student Student { get; set; }
        public virtual MealType MealType { get; set; }
        public virtual DormLocation DormLocation { get; set; }
        public virtual AppUser ScannedByUser { get; set; }
    }
}
