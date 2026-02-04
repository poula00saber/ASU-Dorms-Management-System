using ASUDorms.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASUDorms.Domain.Entities
{
    public class Student : AuditableEntity
    {
        [Required]
        [MaxLength(25)]
        [Key]
        public string NationalId { get; set; } // Can be passport number for foreigners


        [ForeignKey(nameof(DormLocation))]
        public int DormLocationId { get; set; }

        [Required]
        [MaxLength(20)]
        public string StudentId { get; set; }

        // Personal Information
        
        public bool IsEgyptian { get; set; } = true;

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
        public string? PhotoUrl { get; set; }

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
        public string Grade { get; set; } // University grade (e.g., "???", "??? ???")

        [Range(0, 100)]
        public decimal? PercentageGrade { get; set; } // Percentage (e.g., 85.5%)

        // Secondary School Information (for New Students only)
        [MaxLength(200)]
        public string? SecondarySchoolName { get; set; }

        [MaxLength(50)]
        public string? SecondarySchoolGovernment { get; set; }

        [Range(0, 100)]
        public decimal? HighSchoolPercentage { get; set; }

        // Dorm Information
        [Required]
        public DormType DormType { get; set; }

        [Required]
        [MaxLength(10)]
        public string BuildingNumber { get; set; }

        [Required]
        [MaxLength(10)]
        public string RoomNumber { get; set; }

        // Special Needs & Fees
        public bool HasSpecialNeeds { get; set; } = false;

        [MaxLength(500)]
        public string? SpecialNeedsDetails { get; set; }

        public bool IsExemptFromFees { get; set; } = false;

        // Meal Tracking
        public int MissedMealsCount { get; set; } = 0;

        // Payment Information
        public bool HasOutstandingPayment { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingAmount { get; set; } = 0;

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
        public virtual ICollection<PaymentExemption> PaymentExemptions { get; set; }
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; }
    }
}

