using ASUDorms.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Students
{
    public class StudentDto
    {
        public string StudentId { get; set; }
        public string NationalId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public StudentStatus Status { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public Religion Religion { get; set; }
        public string PhotoUrl { get; set; }
        public string Government { get; set; }
        public string District { get; set; }
        public string StreetName { get; set; }
        public string Faculty { get; set; }
        public int Level { get; set; }
        public string Grade { get; set; }
        public DormType DormType { get; set; }
        public string BuildingNumber { get; set; }
        public string RoomNumber { get; set; }
        public bool HasSpecialNeeds { get; set; }
        public string? SpecialNeedsDetails { get; set; }
        public bool IsExemptFromFees { get; set; }
        public string FatherName { get; set; }
        public string FatherNationalId { get; set; }
        public string FatherProfession { get; set; }
        public string FatherPhone { get; set; }
        public string GuardianName { get; set; }
        public string GuardianRelationship { get; set; }
        public string GuardianPhone { get; set; }
        public int DormLocationId { get; set; }
    }
}
