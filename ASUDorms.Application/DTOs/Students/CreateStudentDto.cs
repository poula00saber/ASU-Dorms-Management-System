using ASUDorms.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Students
{
    public class CreateStudentDto
    {
        [Required]
        public string StudentId { get; set; }

        [Required]
        public string NationalId { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public StudentStatus Status { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public Religion Religion { get; set; }

        [Required]
        public string Government { get; set; }

        [Required]
        public string District { get; set; }

        public string StreetName { get; set; }

        [Required]
        public string Faculty { get; set; }
        [Required]
        public int Level { get; set; }
        [Required]
        public string Grade { get; set; }

        [Required]
        public DormType DormType { get; set; }

        [Required]
        public string BuildingNumber { get; set; }

        [Required]
        public string RoomNumber { get; set; }

        public bool HasSpecialNeeds { get; set; }
        public string? SpecialNeedsDetails { get; set; }
        public bool IsExemptFromFees { get; set; }

        [Required]
        public string FatherName { get; set; }

        [Required]
        public string FatherNationalId { get; set; }

        public string FatherProfession { get; set; }

        [Required]
        public string FatherPhone { get; set; }

        [Required]
        public string GuardianName { get; set; }

        [Required]
        public string GuardianRelationship { get; set; }

        [Required]
        public string GuardianPhone { get; set; }

        public string PhotoUrl { get; set; }

    }
}
