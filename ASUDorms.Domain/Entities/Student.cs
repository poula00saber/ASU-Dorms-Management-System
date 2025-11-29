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
    public class Student : BaseEntity
    {
        [Key]
        [MaxLength(20)]
        public string StudentId { get; set; }

        // Personal Information
        [Required]
        [MaxLength(14)]
        public string NationalId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        public StudentStatus Status { get; set; }

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        public Religion Religion { get; set; }

        [MaxLength(500)]
        public string PhotoUrl { get; set; }

        // Address Information
        [Required]
        [MaxLength(50)]
        public string Government { get; set; }

        [Required]
        [MaxLength(50)]
        public string District { get; set; }

        [MaxLength(200)]
        public string StreetName { get; set; }

        // Academic Information
        [Required]
        [MaxLength(100)]
        public string Faculty { get; set; }

        [Required]
        [Range(1, 5)]
        public int Level { get; set; }

        [Required]
        [MaxLength(20)]
        public string Grade { get; set; }

        // Dorm Information
        [Required]
        public DormType DormType { get; set; }

        [Required]
        [MaxLength(10)]
        public string BuildingNumber { get; set; }

        [Required]
        [MaxLength(10)]
        public string RoomNumber { get; set; }

        [Required]
        [ForeignKey(nameof(DormLocation))]
        public int DormLocationId { get; set; }

        // Special Needs & Fees
        public bool HasSpecialNeeds { get; set; } = false;

        [MaxLength(500)]
        public string? SpecialNeedsDetails { get; set; }

        public bool IsExemptFromFees { get; set; } = false;

        // Family Information
        [Required]
        [MaxLength(100)]
        public string FatherName { get; set; }

        [Required]
        [MaxLength(14)]
        public string FatherNationalId { get; set; }

        [MaxLength(100)]
        public string FatherProfession { get; set; }

        [Required]
        [MaxLength(20)]
        public string FatherPhone { get; set; }

        [Required]
        [MaxLength(100)]
        public string GuardianName { get; set; }

        [Required]
        [MaxLength(50)]
        public string GuardianRelationship { get; set; }

        [Required]
        [MaxLength(20)]
        public string GuardianPhone { get; set; }

        // Navigation Properties
        public virtual DormLocation DormLocation { get; set; }
        public virtual ICollection<Holiday> Holidays { get; set; }
        public virtual ICollection<MealTransaction> MealTransactions { get; set; }
    }
}
